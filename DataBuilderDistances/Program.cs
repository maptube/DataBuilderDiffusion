//DataBuilder for Mike's distance and time from charing cross file (or any centre point)
//Charing Cross is in Westminster 018, E02000977

//data format is:
//Zone-- - Employment-- - population-- - land area, —x, —y,  travel time from Charing X to each zone
//1
//2
//3
//.
//.
//.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using QUANT3Core;
using QUANT3Core.utils;
using System.Data;

//configure a centre point for the data i.e. you can set any point
const string CentreZoneCode = "E02000977"; //Charing Cross, Westminster 018

var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory) //or Directory.GetCurrentDirectory
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

IConfigurationRoot configuration = builder.Build();

string ModelRunsDir = configuration["dirs:ModelRunsDir"];
string ModelRunsUsersDir = configuration["dirs:ModelRunsUsersDir"];
string ModelPrefix = configuration["modelprefix"];
ModelRunsDir = Path.Combine(ModelRunsDir, ModelPrefix);

//this is the file the scenario uses
string NullRoadDisFilename = Path.Combine(ModelRunsDir, "null_dis_roads_min.bin"); //mode 1 null

//not used??
string RoadDisFilename = Path.Combine(ModelRunsDir, "dis_roads_min.bin"); //mode 1

//Observed flows on all modes, NOT model
string TObsFilename = Path.Combine(ModelRunsDir, configuration["matrices:TObs"]);

string ZoneCodesFilename = configuration["tables:ZoneCodes"];
DataTable dtZoneCodes = Serialiser.GetXMLDataTable(Path.Combine(ModelRunsDir, ZoneCodesFilename));
dtZoneCodes.PrimaryKey = new DataColumn[] { dtZoneCodes.Columns["zonei"] }; //note zonei pk

//now do the work...

//flows
FMatrix TObs = FMatrix.DirtyDeserialise(TObsFilename);
float[] Oi = TObs.ComputeOi();
float[] Dj = TObs.ComputeDj();

//costs
FMatrix disRoad = FMatrix.DirtyDeserialise(NullRoadDisFilename); //road null

//we need to know what the zone index of the CentreZone is - pk is wrong so do it the bad way:
int CentreZone_i = -1;
float CentreZone_east = 0;
float CentreZone_north = 0;
foreach (DataRow row in dtZoneCodes.Rows)
{
    string? areakey = row["areakey"] as string;
    if (areakey==CentreZoneCode)
    {
        CentreZone_i = (int)row["zonei"];
        CentreZone_east = (float)row["osgb36_east"];
        CentreZone_north = (float)row["osgb36_north"];
        break;
    } 
}

string Filename = "MinutesFrom" + CentreZoneCode + ".csv";
Console.WriteLine("writing file: "+Filename);
using (StreamWriter writer = new StreamWriter(Filename))
{
    //output:
    //zonecode, employment, residential, landarea, lng, lat, east, north, RoadMinsFromExxxxx
    writer.WriteLine("zonei, zonecode, employment(Oi), residential(Dj), landarea(m^2), lng, lat, east, north, crowflydist(KM), RoadMinsFrom"+CentreZoneCode);

    foreach (DataRow row in dtZoneCodes.Rows)
    {
        string? areakey = row["areakey"] as string;
        int zonei = (int)row["zonei"];
        float lat = (float)row["lat"];
        float lon = (float)row["lon"];
        float east = (float)row["osgb36_east"];
        float north = (float)row["osgb36_north"];
        float area_m2 = (float)row["area"]; //this is metres squared City of London area is 2.9KM^2, *1,000,000 gives ~2897837.5 which is our data point
        //now the added data - Oi and Dj are easy, fill in road mins from CentreZone here
        //crowfly dist added for additional checking
        float dx = east - CentreZone_east;
        float dy = north - CentreZone_north;
        float crowflyKM = (float)Math.Sqrt(dx * dx + dy * dy)/1000.0f;
        //Remember model is work to home e.g. dis[workmsoa,homemsoa]
        //so this is a worker in centrezone going home to zonei so
        //time is therefore centerzone->zonei time, not other way around
        float roadmins = disRoad._M[CentreZone_i, zonei]; //work,home - it's always origin to destination
        writer.WriteLine(
            string.Format(
                "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                zonei, areakey, Oi[zonei], Dj[zonei], area_m2,
                lon, lat, east, north, crowflyKM, roadmins)
        );

    }
}

//finished...