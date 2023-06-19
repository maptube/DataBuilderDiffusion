//Data Builder for the diffusion data requied by Elsa
//
//Data format:
//MSOA1            MSOA2            time_car    time_bus    time_rail   HS2   flow
//id number      id number      time mins       time mins       time mins    0 or 1    total n. people
//
//So, basically, merge QUANT matrices together in just the right way
//
//THIS IS THE REAL FORMAT!
//DEST_WorkMSOA, ORIGIN_HomeMSOA, RoadMins, BusMins, RailMins, HS2IRPRailMins, TotalFlow(people)
//HS2IRP column added next to rail mins to carry the IRP rail time

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using QUANT3Core;
using QUANT3Core.utils;
using System.Data;

Console.WriteLine("Hello, World!");

var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory) //or Directory.GetCurrentDirectory
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

IConfigurationRoot configuration = builder.Build();

string ModelRunsDir = configuration["dirs:ModelRunsDir"];
string ModelRunsUsersDir = configuration["dirs:ModelRunsUsersDir"];
string ModelPrefix = configuration["modelprefix"];
ModelRunsDir = Path.Combine(ModelRunsDir, ModelPrefix);

//NOTE: we use the null scenario matrices here for differencing with the scenarion data - these are the APSP code re-run using the graphml networks
string NullGBRailDisFilename = Path.Combine(ModelRunsDir, "null_dis_gbrail_min.bin"); //mode 3 null
string NullBusDisFilename = Path.Combine(ModelRunsDir, "null_dis_bus_min.bin"); //mode 2 null
string NullRoadDisFilename = Path.Combine(ModelRunsDir, "null_dis_roads_min.bin"); //mode 1 null

string GBRailDisFilename = Path.Combine(ModelRunsDir, "dis_gbrail_min.bin"); //mode 3
string BusDisFilename = Path.Combine(ModelRunsDir, "dis_bus_min.bin"); //mode 2
string RoadDisFilename = Path.Combine(ModelRunsDir, "dis_roads_min.bin"); //mode 1


//Observed flows on each mode, NOT model
string TObs1Filename = Path.Combine(ModelRunsDir, configuration["matrices:TObs1"]);
string TObs2Filename = Path.Combine(ModelRunsDir, configuration["matrices:TObs2"]);
string TObs3Filename = Path.Combine(ModelRunsDir, configuration["matrices:TObs3"]);

string ZoneCodesFilename = configuration["tables:ZoneCodes"];
DataTable dtZoneCodes = Serialiser.GetXMLDataTable(Path.Combine(ModelRunsDir, ZoneCodesFilename));
dtZoneCodes.PrimaryKey = new DataColumn[] { dtZoneCodes.Columns["zonei"] }; //note zonei pk

//now do the work...

//flows
FMatrix TObsRoad = FMatrix.DirtyDeserialise(TObs1Filename);
FMatrix TObsBus = FMatrix.DirtyDeserialise(TObs2Filename);
FMatrix TObsRail = FMatrix.DirtyDeserialise(TObs3Filename);

//costs
FMatrix disRoad = FMatrix.DirtyDeserialise(NullRoadDisFilename); //road null
FMatrix disBus = FMatrix.DirtyDeserialise(NullBusDisFilename); //bus null
FMatrix disGBRail = FMatrix.DirtyDeserialise(NullGBRailDisFilename); //rail null

//HS2 costs - only rail changes as it's a rail scenario
FMatrix HS2IRP_disGBRail = FMatrix.DirtyDeserialise(
    Path.Combine(ModelRunsUsersDir,
        "scenariorunner_HS2IRP_20221011_153058/modelrun_20221011_153308/dis3.bin")
);


//write out zone codes
using (StreamWriter writer = new StreamWriter("zonecodes.csv"))
{
    writer.WriteLine("zonei,areakey,lat,lon,osgb36_east,osgb36_north,area");
    foreach (DataRow row in dtZoneCodes.Rows)
    {
        string areakey = row["areakey"] as string;
        int zonei = (int)row["zonei"];
        float lat = (float)row["lat"];
        float lon = (float)row["lon"];
        float east = (float)row["osgb36_east"];
        float north = (float)row["osgb36_north"];
        float area = (float)row["area"];
        writer.WriteLine(
            string.Format(
                "{0},{1},{2},{3},{4},{5},{6}",
                zonei, areakey, lat, lon, east, north, area)
        );
    }
}

//write out gbrail time distribution, but cut off the lower part of the curve
Console.WriteLine("writing distribution");
int N = disGBRail.N;
using (StreamWriter writer = new StreamWriter("gbrail-distribution.csv"))
{
    writer.WriteLine("mins");
    for (int i = 0; i < N; i++)
    {
        for (int j = 0; j < N; j++)
        {
            if (disGBRail._M[i, j] > 200) writer.WriteLine(disGBRail._M[i, j]);
        }
    }
}

//call the cut-off 120 mins

float cutoffrailmins = 120;

//now onto the big csv production
Console.WriteLine("writing trips cost csv");
int count = 0;
using (StreamWriter writer = new StreamWriter("quant-transit-times.csv"))
{
    writer.WriteLine("DEST_WorkMSOA, ORIGIN_HomeMSOA, RoadMins, BusMins, RailMins, HS2IRPRailMins, TotalFlow(people)");
    for (int i=0; i<N; i++)
    {
        DataRow Rowi = dtZoneCodes.Rows.Find(i);
        string MSOAi = Rowi["areakey"] as string;
        for (int j=0; j<N; j++)
        {
            float Cij_gbrail = disGBRail._M[i, j];
            float Cij_HS2IRP_gbrail = HS2IRP_disGBRail._M[i, j];
            if ((Cij_gbrail < cutoffrailmins) || (Cij_HS2IRP_gbrail < cutoffrailmins))
            {
                ++count;
                DataRow Rowj = dtZoneCodes.Rows.Find(j);
                string MSOAj = Rowj["areakey"] as string;
                float Cij_road = disRoad._M[i, j];
                float Cij_bus = disBus._M[i, j];
                float Tij_road = TObsRoad._M[i, j];
                float Tij_bus = TObsBus._M[i, j];
                float Tij_rail = TObsRail._M[i, j];
                float Tij = Tij_road + Tij_bus + Tij_rail;

                writer.WriteLine(
                    string.Format(
                        "{0},{1},{2:F2},{3:F2},{4:F2},{5:F2},{6}",
                        MSOAi, MSOAj, Cij_road, Cij_bus, Cij_gbrail, Cij_HS2IRP_gbrail, Tij
                    )
                );
            }
        }
    }
}
Console.WriteLine("count=" + count);