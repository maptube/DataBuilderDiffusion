//QUANT3 project at LSOA level for Roberto/Clementine paper
//Due to the LSOA data, this is a databuilder project and QUANT3 calibration.
//Data is supplied at LSOA level for Liverpool, Leeds, Manchester, Birmingham, London.

//SEQUENCE
//Databuilder: make zonecodes from shapefile
//Databuilder: make cost matrices for three modes to new centroids
//Databuilder: make flow matrices from csv
//QUANT3: calibrate beta and make predicted matrices

using System.Data;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Configuration.Json;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.KdTree;
using QuickGraph;
using QuickGraph.Algorithms;
using QUANT2Core;
using QUANT2Core.networks;
using DataBuilder2;
using QUANT3Core.utils; //needed for the new zonecodes load now it's not binary
using QUANT3;
using QUANT3LSOA;
using System.Net.NetworkInformation;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

string ModelRunsDir = "../../../model-runs";
//todo: needs graphs and shapefiles for transport networks under model-runs
string bham_AreaShapefileName = "../../../data/ZonesBirmingham_WGS84.shp"; //NOTE: there is a non WGS84 version which is the original
string livman_AreaShapefileName = "../../../data/ZonesLiverpoolManchester_WGS84.shp";
string london_AreaShapefileName = "../../../data/ZonesLondon_WGS84.shp";
string londonboroughMSOA_AreaShapefileName = "../../../data/LondonBoroughsMSOA_WGS84.shp";
string londonboroughLSOA_AreaShapefileName = "../../../data/LondonBoroughsLSOA_WGS84.shp";
string FlowCSVFilename = "../../../data/lsoaCommutebyMode.csv";
string FlowCSVFilenameMSOA = "../../../data/wu03uk_v3.csv";


//HACK! - generate LSOA model to MSOA areas aggregation data - Mike, Montreal June 2023
//DataBuilderLSOA.GenerateLSOAAggregateToMSOA();

////////////////////////////////////////////////////////////////////////////////
//Birmingham data - bham prefix
//DataBuilderLSOA builder = new DataBuilderLSOA()
//{
//    ModelRunsDir = ModelRunsDir,
//    Prefix = "bham",
//    ZoneCodesLookupFilename = "zonecodes.xml"
//};

//builder.BuildPrefixDataFromShapefile(
//    bham_AreaShapefileName, "LSOA11CD", "LSOA11NM", FlowCSVFilename
//);
////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
//Liverpool Manchester data - livman prefix
//DataBuilderLSOA builder2 = new DataBuilderLSOA()
//{
//    ModelRunsDir = ModelRunsDir,
//    Prefix = "livman",
//    ZoneCodesLookupFilename = "zonecodes.xml"
//};

//builder2.BuildPrefixDataFromShapefile(
//    livman_AreaShapefileName, "LSOA11CD", "LSOA11NM", FlowCSVFilename
//);
////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
//London data - london prefix
//DataBuilderLSOA builder3 = new DataBuilderLSOA()
//{
//    ModelRunsDir = ModelRunsDir,
//    Prefix = "london",
//    ZoneCodesLookupFilename = "zonecodes.xml"
//};

//builder3.BuildPrefixDataFromShapefile(
//    london_AreaShapefileName, "LSOA11CD", "LSOA11NM", FlowCSVFilename
//);
////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
//London Borough data - lonboro_msoa prefix
//DataBuilderLSOA builder3 = new DataBuilderLSOA()
//{
//    ModelRunsDir = ModelRunsDir,
//    Prefix = "lonboro_msoa",
//    ZoneCodesLookupFilename = "zonecodes.xml"
//};

//builder3.BuildPrefixDataFromShapefile(
//    londonboroughMSOA_AreaShapefileName, "MSOA11CD", "MSOA11NM", FlowCSVFilenameMSOA
//);
////////////////////////////////////////////////////////////////////////////////


////////////////////////////////////////////////////////////////////////////////
//London Borough data - lonboro_lsoa prefix
//DataBuilderLSOA builder3 = new DataBuilderLSOA()
//{
//    ModelRunsDir = ModelRunsDir,
//    Prefix = "lonboro_lsoa",
//    ZoneCodesLookupFilename = "zonecodes.xml"
//};

//builder3.BuildPrefixDataFromShapefile(
//    londonboroughLSOA_AreaShapefileName, "LSOA11CD", "LSOA11NM", FlowCSVFilename
//);
////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
//Now calibrate the three models
//string[] prefixes = { "bham", "livman", "london" };
string[] prefixes = { "lonboro_msoa", "lonboro_lsoa" };
foreach (string prefix in prefixes)
{
    Console.WriteLine();
    Console.WriteLine("================================================================================");
    Console.WriteLine("prefix=" + prefix);
    QUANT3Core.models.QUANT3ModelProperties q3mp = new QUANT3Core.models.QUANT3ModelProperties();
    q3mp.InTObs = new string[] {
        Path.Combine(ModelRunsDir,prefix+"/TObs_1.bin"),
        Path.Combine(ModelRunsDir,prefix+"/TObs_2.bin"),
        Path.Combine(ModelRunsDir,prefix+"/TObs_3.bin")
    };
    q3mp.Indis = new string[]
    {
        Path.Combine(ModelRunsDir, prefix+"/dis_roads_min.bin"),
        Path.Combine(ModelRunsDir, prefix+"/dis_bus_min.bin"),
        Path.Combine(ModelRunsDir, prefix+"/dis_gbrail_min.bin")
    };
    q3mp.IsUsingConstraints = false;
    //create and run
    QUANT3Core.models.QUANT3Model QM3 = new QUANT3Core.models.QUANT3Model();
    QM3.InitialiseFromProperties(q3mp);
    QM3.IsUsingConstraints = false;
    QM3.Run();
    for (int i = 0; i < 3; i++) //write out calibrated data
        QM3.TPred[i].DirtySerialise(Path.Combine(ModelRunsDir, prefix+"/TPred_Q3_"+(i+1)+".bin"));
    Console.WriteLine("beta: " + QM3.Beta[0] +   " (road),  "  +  QM3.Beta[1] + " (bus),  " + QM3.Beta[2]+" (rail)");
    string[] strMode = { "road", "bus", "rail" };
    for (int i=0; i<3; i++)
    {
        float CBarObs = QM3.CalculateCBar(ref QM3.TObs[i], ref QM3.dis[i]);
        float CBarPred = QM3.CalculateCBar(ref QM3.TPred[i], ref QM3.dis[i]);
        Console.WriteLine(strMode[i] + ": CBarObs=" + CBarObs + " CBarPred="+CBarPred + " delta=" + (CBarPred - CBarObs));
    }

    //generate data
    QUANT3Core.FMatrix [] dijKM = new QUANT3Core.FMatrix[] {
        QUANT3Core.FMatrix.DirtyDeserialise(Path.Combine(ModelRunsDir,prefix+"/dis_crowfly_vertex_roads_KM.bin")),
        QUANT3Core.FMatrix.DirtyDeserialise(Path.Combine(ModelRunsDir,prefix+"/dis_crowfly_vertex_bus_KM.bin")),
        QUANT3Core.FMatrix.DirtyDeserialise(Path.Combine(ModelRunsDir,prefix+"/dis_crowfly_vertex_gbrail_KM.bin"))
    };
    DataTable ZoneCodes = Serialiser.GetXMLDataTable(Path.Combine(ModelRunsDir, prefix+"/zonecodes.xml"));
    QUANT3Core.statistics.impacts.ImpactStatistics impacts = new QUANT3Core.statistics.impacts.ImpactStatistics();
    impacts.ComputeFromData(true, QM3.TObs, QM3.dis, QM3.TPred, QM3.dis, dijKM, null, ZoneCodes);
    impacts.WriteData(Path.Combine(ModelRunsDir, prefix+"/ImpactsScalar_"+prefix+".csv"), Path.Combine(ModelRunsDir, prefix+"/ImpactsVector_"+prefix+".csv"),ZoneCodes);
    //QUANT3.Server.NamedDataSources NDS = new QUANT3.Server.NamedDataSources(null,null);
    //DataTable dt = NDS.OiObs(ModelRunsDir, ModelRunsDir, QUANT3.Server.NamedDataSources.NamedVariableModeData.All, true);
    //Serialiser.PutXML("testOiObs.csv", dt);
    
}

////////////////////////////////////////////////////////////////////////////////
/*

================================================================================
prefix=bham
beta: 0.28054968 (road),  0.09312309 (bus),  0.08320994 (rail)
road: CBarObs=6.337004 CBarPred=6.331252 delta=-0.0057520866
bus: CBarObs=20.383644 CBarPred=20.370277 delta=-0.013366699
rail: CBarObs=15.727967 CBarPred=15.716785 delta=-0.011181831

================================================================================
prefix=livman
beta: 0.24682193 (road),  0.09551606 (bus),  0.103540435 (rail)
road: CBarObs=7.444474 CBarPred=7.443612 delta=-0.0008621216
bus: CBarObs=20.515234 CBarPred=20.519995 delta=0.004760742
rail: CBarObs=17.59194 CBarPred=17.598248 delta=0.006307602

================================================================================
prefix=london
beta: 0.2294048 (road),  0.07461734 (bus),  0.09305321 (rail)
road: CBarObs=7.404443 CBarPred=7.399432 delta=-0.005010605
bus: CBarObs=22.386852 CBarPred=22.365408 delta=-0.02144432
rail: CBarObs=16.80096 CBarPred=16.78805 delta=-0.012910843

================================================================================
prefix=lonboro_msoa
beta: 0.29516986 (road),  0.082540214 (bus),  0.05710329 (rail)
road: CBarObs=5.9474134 CBarPred=5.951098 delta=0.0036845207
bus: CBarObs=25.13583 CBarPred=25.126375 delta=-0.009454727
rail: CBarObs=21.582283 CBarPred=21.60186 delta=0.019577026

================================================================================
prefix=lonboro_lsoa
beta: 0.29384002 (road),  0.07074919 (bus),  0.078371555 (rail)
road: CBarObs=5.9150386 CBarPred=5.91275 delta=-0.0022888184
bus: CBarObs=23.049131 CBarPred=23.026358 delta=-0.022773743
rail: CBarObs=15.217654 CBarPred=15.203089 delta=-0.014565468

*/

////////////////////////////////////////////////////////////////////////////////
