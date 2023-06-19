using NetTopologySuite.Geometries;
using NetTopologySuite.Index.KdTree;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.KdTree;
using QuickGraph;
using QuickGraph.Algorithms;
using QUANT2Core;
using QUANT2Core.networks;
using DataBuilder2;
using QUANT3Core.utils; //needed for the new zonecodes load now it's not binary
using QUANT2Core.io;

namespace QUANT3LSOA
{
    public class DataBuilderLSOA
    {
        public string ModelRunsDir = ""; //must set
        public string Prefix = ""; //must set
        public string PrefixModelRunsDir = ""; //ModelRunsDir+Prefix
        public string ZoneCodesLookupFilename = "zonecodes.xml";
        public string TObs1Filename = "TObs_1.bin";
        public string TObs2Filename = "TObs_2.bin";
        public string TObs3Filename = "TObs_3.bin";

        /// <summary>
        /// This is a big hack to generate the LSOA London model but aggregated down
        /// to the MSOA model regions so there is a direct comparison between the
        /// same model but at two different spatial scales.
        /// This was originally for Mike's Digital Twins talk going to Montreal June 2023.
        /// PRE: requires two files: LSOAMSOAAggregateData.csv and ImpactsVector_lonboro_msoa.csv
        /// LSOAMSOAAggregateData.csv is obtained by using QGIS to join the impacts data
        /// from the model run with the LSOA London shapefile, which also contains the
        /// MSOA code that the LSOA sits inside for every row so this is what is used
        /// to aggregate up to MSOA level.
        /// NOTE: you get the file from QGIS after joining the shapefile with the
        /// ImpactsVector_lonboro_lsoa.csv (in model-runs/lonboro_lsoa) by copying
        /// and pasting the entire contents of the Attribute Table into Excel and saving
        /// as a CSV file.
        /// Then you just have to add up all the numbers for each unique MSOA listed
        /// e.g. group by MSOA code. 
        /// NOTE: the impacts data has 1=observed data and 2=predicted data, so
        /// ImpactsVector_lonboro_lsoa_Cj2 is Dj predicted as C is a people count by j.
        /// Having got the LSOA model now aggregated to MSOA, you then need to load the
        /// MSOA model data so you can compare with its data.
        /// ImpactsVector_lonboro_msoa.csv is the impacts file generated from the
        /// MSOA London model run. This will be in the model-runs directory under
        /// "lonboro_msoa" and is automatically produced by the model run.
        /// 
        /// Finally, we can run the function below and it gives us the following:
        /// LSOAModelAggregatedToMSOA (0),MSOAModel (1),Difference ([0]-[1]),Percentage Difference ([0]-[1])/[1]*100%
        /// 
        /// The output goes to the debug console, so you will need to cut and paste it
        /// into a csv file for further analysis e.g. back into QGIS.
        /// </summary>
        public static void GenerateLSOAAggregateToMSOA()
        {
            //hack - aggregate data
            DataTable dtLSOA = new DataTable();
            //LSOA11CD,LSOA11NM,MSOA11CD,MSOA11NM,LAD11CD,LAD11NM,RGN11CD,RGN11NM,USUALRES,HHOLDRES,COMESTRES,POPDEN,HHOLDS,AVHHOLDSZ,area,ImpactsVector_lonboro_lsoa_zonei,ImpactsVector_lonboro_lsoa_Cik1_road,ImpactsVector_lonboro_lsoa_Cik1_bus,ImpactsVector_lonboro_lsoa_Cik1_rail,ImpactsVector_lonboro_lsoa_Cik2_road,ImpactsVector_lonboro_lsoa_Cik2_bus,ImpactsVector_lonboro_lsoa_Cik2_rail,ImpactsVector_lonboro_lsoa_CikDiff_road,ImpactsVector_lonboro_lsoa_CikDiff_bus,ImpactsVector_lonboro_lsoa_CikDiff_rail,ImpactsVector_lonboro_lsoa_Cjk1_road,ImpactsVector_lonboro_lsoa_Cjk1_bus,ImpactsVector_lonboro_lsoa_Cjk1_rail,ImpactsVector_lonboro_lsoa_Cjk2_road,ImpactsVector_lonboro_lsoa_Cjk2_bus,ImpactsVector_lonboro_lsoa_Cjk2_rail,ImpactsVector_lonboro_lsoa_CjkDiff_road,ImpactsVector_lonboro_lsoa_CjkDiff_bus,ImpactsVector_lonboro_lsoa_CjkDiff_rail,ImpactsVector_lonboro_lsoa_Ci1,ImpactsVector_lonboro_lsoa_Ci2,ImpactsVector_lonboro_lsoa_CiDiff,ImpactsVector_lonboro_lsoa_Cj1,ImpactsVector_lonboro_lsoa_Cj2,ImpactsVector_lonboro_lsoa_CjDiff,ImpactsVector_lonboro_lsoa_CiBark1_road,ImpactsVector_lonboro_lsoa_CiBark1_bus,ImpactsVector_lonboro_lsoa_CiBark1_rail,ImpactsVector_lonboro_lsoa_CiBark2_road,ImpactsVector_lonboro_lsoa_CiBark2_bus,ImpactsVector_lonboro_lsoa_CiBark2_rail,ImpactsVector_lonboro_lsoa_CiBarkDiff_road,ImpactsVector_lonboro_lsoa_CiBarkDiff_bus,ImpactsVector_lonboro_lsoa_CiBarkDiff_rail,ImpactsVector_lonboro_lsoa_CiBarkPCTDiff_road,ImpactsVector_lonboro_lsoa_CiBarkPCTDiff_bus,ImpactsVector_lonboro_lsoa_CiBarkPCTDiff_rail,ImpactsVector_lonboro_lsoa_CiBar1,ImpactsVector_lonboro_lsoa_CiBar2,ImpactsVector_lonboro_lsoa_CiBarDiff,ImpactsVector_lonboro_lsoa_CiBarPCTDiff,ImpactsVector_lonboro_lsoa_CjBark1_road,ImpactsVector_lonboro_lsoa_CjBark1_bus,ImpactsVector_lonboro_lsoa_CjBark1_rail,ImpactsVector_lonboro_lsoa_CjBark2_road,ImpactsVector_lonboro_lsoa_CjBark2_bus,ImpactsVector_lonboro_lsoa_CjBark2_rail,ImpactsVector_lonboro_lsoa_CjBarkDiff_road,ImpactsVector_lonboro_lsoa_CjBarkDiff_bus,ImpactsVector_lonboro_lsoa_CjBarkDiff_rail,ImpactsVector_lonboro_lsoa_CjBarkPCTDiff_road,ImpactsVector_lonboro_lsoa_CjBarkPCTDiff_bus,ImpactsVector_lonboro_lsoa_CjBarkPCTDiff_rail,ImpactsVector_lonboro_lsoa_CjBar1,ImpactsVector_lonboro_lsoa_CjBar2,ImpactsVector_lonboro_lsoa_CjBarDiff,ImpactsVector_lonboro_lsoa_CjBarPCTDiff,ImpactsVector_lonboro_lsoa_Lik1_road,ImpactsVector_lonboro_lsoa_Lik1_bus,ImpactsVector_lonboro_lsoa_Lik1_rail,ImpactsVector_lonboro_lsoa_Lik2_road,ImpactsVector_lonboro_lsoa_Lik2_bus,ImpactsVector_lonboro_lsoa_Lik2_rail,ImpactsVector_lonboro_lsoa_LikDiff_road,ImpactsVector_lonboro_lsoa_LikDiff_bus,ImpactsVector_lonboro_lsoa_LikDiff_rail,ImpactsVector_lonboro_lsoa_LikPCTDiff_road,ImpactsVector_lonboro_lsoa_LikPCTDiff_bus,ImpactsVector_lonboro_lsoa_LikPCTDiff_rail,ImpactsVector_lonboro_lsoa_Ljk1_road,ImpactsVector_lonboro_lsoa_Ljk1_bus,ImpactsVector_lonboro_lsoa_Ljk1_rail,ImpactsVector_lonboro_lsoa_Ljk2_road,ImpactsVector_lonboro_lsoa_Ljk2_bus,ImpactsVector_lonboro_lsoa_Ljk2_rail,ImpactsVector_lonboro_lsoa_LjkDiff_road,ImpactsVector_lonboro_lsoa_LjkDiff_bus,ImpactsVector_lonboro_lsoa_LjkDiff_rail,ImpactsVector_lonboro_lsoa_LjkPCTDiff_road,ImpactsVector_lonboro_lsoa_LjkPCTDiff_bus,ImpactsVector_lonboro_lsoa_LjkPCTDiff_rail,ImpactsVector_lonboro_lsoa_alphai,ImpactsVector_lonboro_lsoa_alphaj,ImpactsVector_lonboro_lsoa_niPlus_road,ImpactsVector_lonboro_lsoa_niZero_road,ImpactsVector_lonboro_lsoa_niMinus_road,ImpactsVector_lonboro_lsoa_niPlus_bus,ImpactsVector_lonboro_lsoa_niZero_bus,ImpactsVector_lonboro_lsoa_niMinus_bus,ImpactsVector_lonboro_lsoa_niPlus_rail,ImpactsVector_lonboro_lsoa_niZero_rail,ImpactsVector_lonboro_lsoa_niMinus_rail,ImpactsVector_lonboro_lsoa_njPlus_road,ImpactsVector_lonboro_lsoa_njZero_road,ImpactsVector_lonboro_lsoa_njMinus_road,ImpactsVector_lonboro_lsoa_njPlus_bus,ImpactsVector_lonboro_lsoa_njZero_bus,ImpactsVector_lonboro_lsoa_njMinus_bus,ImpactsVector_lonboro_lsoa_njPlus_rail,ImpactsVector_lonboro_lsoa_njZero_rail,ImpactsVector_lonboro_lsoa_njMinus_rail,ImpactsVector_lonboro_lsoa_Rhoi_road,ImpactsVector_lonboro_lsoa_Rhoi_bus,ImpactsVector_lonboro_lsoa_Rhoi_rail,ImpactsVector_lonboro_lsoa_RhoiPlus_road,ImpactsVector_lonboro_lsoa_RhoiPlus_bus,ImpactsVector_lonboro_lsoa_RhoiPlus_rail,ImpactsVector_lonboro_lsoa_RhoiMinus_road,ImpactsVector_lonboro_lsoa_RhoiMinus_bus,ImpactsVector_lonboro_lsoa_RhoiMinus_rail,ImpactsVector_lonboro_lsoa_Rhoj_road,ImpactsVector_lonboro_lsoa_Rhoj_bus,ImpactsVector_lonboro_lsoa_Rhoj_rail,ImpactsVector_lonboro_lsoa_RhojPlus_road,ImpactsVector_lonboro_lsoa_RhojPlus_bus,ImpactsVector_lonboro_lsoa_RhojPlus_rail,ImpactsVector_lonboro_lsoa_RhojMinus_road,ImpactsVector_lonboro_lsoa_RhojMinus_bus,ImpactsVector_lonboro_lsoa_RhojMinus_rail,ImpactsVector_lonboro_lsoa_di_road,ImpactsVector_lonboro_lsoa_di_bus,ImpactsVector_lonboro_lsoa_di_rail,ImpactsVector_lonboro_lsoa_dj_road,ImpactsVector_lonboro_lsoa_dj_bus,ImpactsVector_lonboro_lsoa_dj_rail,ImpactsVector_lonboro_lsoa_deltaihat_road,ImpactsVector_lonboro_lsoa_deltaihat_bus,ImpactsVector_lonboro_lsoa_deltaihat_rail,ImpactsVector_lonboro_lsoa_deltai_road,ImpactsVector_lonboro_lsoa_deltai_bus,ImpactsVector_lonboro_lsoa_deltai_rail,ImpactsVector_lonboro_lsoa_deltaj_road,ImpactsVector_lonboro_lsoa_deltaj_bus,ImpactsVector_lonboro_lsoa_deltaj_rail,ImpactsVector_lonboro_lsoa_deltajhat_road,ImpactsVector_lonboro_lsoa_deltajhat_bus,ImpactsVector_lonboro_lsoa_deltajhat_rail,ImpactsVector_lonboro_lsoa_ti_road,ImpactsVector_lonboro_lsoa_ti_bus,ImpactsVector_lonboro_lsoa_ti_rail,ImpactsVector_lonboro_lsoa_tihat_road,ImpactsVector_lonboro_lsoa_tihat_bus,ImpactsVector_lonboro_lsoa_tihat_rail,ImpactsVector_lonboro_lsoa_tj_road,ImpactsVector_lonboro_lsoa_tj_bus,ImpactsVector_lonboro_lsoa_tj_rail,ImpactsVector_lonboro_lsoa_tjhat_road,ImpactsVector_lonboro_lsoa_tjhat_bus,ImpactsVector_lonboro_lsoa_tjhat_rail,ImpactsVector_lonboro_lsoa_chiik_road,ImpactsVector_lonboro_lsoa_chiik_bus,ImpactsVector_lonboro_lsoa_chiik_rail,ImpactsVector_lonboro_lsoa_chijk_road,ImpactsVector_lonboro_lsoa_chijk_bus,ImpactsVector_lonboro_lsoa_chijk_rail,ImpactsVector_lonboro_lsoa_chii,ImpactsVector_lonboro_lsoa_chij
            //E01000001,City of London 001A,E02000001,City of London 001,E09000001,City of London,E12000007,London,1465,1465,0,112.9,876,1.7,133342.921,0,506,671,6679,1906.828014,1300.661738,4648.510247,1400.828014,629.6617381,-2030.489753,31,30,182,148.9454472,100.4819842,209.660158,117.9454472,70.48198419,27.66015795,7856,7855.999999,-1.37E-06,243,459.0875894,216.0875894,6.988373136,25.59719902,16.1514554,5.445910225,25.14431011,13.35004086,-1.54246291,-0.452888915,-2.801414536,-0.220718453,-0.017692909,-0.17344657,48.73702756,43.9402612,-4.796766361,-0.098421397,3.103069478,15.59661488,11.47531802,2.94621794,14.93091696,11.36933456,-0.156851539,-0.66569792,-0.105983456,-0.050547221,-0.042682205,-0.009235775,30.17500237,29.24646946,-0.928532915,-0.030771594,4719.425694,3739.910043,74541.99858,13738.2097,9632.513322,45397.34012,9018.784005,5892.603279,-29144.65846,1.910991843,1.575600271,-0.390983057,123.9499902,84.13119245,915.5558134,561.0799611,313.0654542,1477.497451,437.1299709,228.9342617,561.9416381,3.526664023,2.721157933,0.613771034,0.014198897,0.004805395,0,4835,0,0,4835,0,0,4835,0,0,4835,0,0,4835,0,0,4835,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,nan,nan,nan,nan,nan,nan,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
            using (StreamReader sr = new StreamReader("C:\\richard\\github\\DataBuilderDiffusion\\QUANT3LSOA\\data\\LSOAMSOAAggregateData.csv"))
            {
                string[] headers = sr.ReadLine().Split(',');
                foreach (string header in headers)
                {
                    try
                    {
                        dtLSOA.Columns.Add(header);
                    }
                    catch { }
                }
                while (!sr.EndOfStream)
                {
                    string[] rows = sr.ReadLine().Split(',');
                    DataRow dr = dtLSOA.NewRow();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = rows[i];
                    }
                    dtLSOA.Rows.Add(dr);
                }

            }
            //and load the msoa model
            DataTable dtMSOA = new DataTable();
            //zonei,areakey,Cik1_road,Cik1_bus,Cik1_rail,Cik2_road,Cik2_bus,Cik2_rail,CikDiff_road,CikDiff_bus,CikDiff_rail,Cjk1_road,Cjk1_bus,Cjk1_rail,Cjk2_road,Cjk2_bus,Cjk2_rail,CjkDiff_road,CjkDiff_bus,CjkDiff_rail,Ci1,Ci2,CiDiff,Cj1,Cj2,CjDiff,CiBark1_road,CiBark1_bus,CiBark1_rail,CiBark2_road,CiBark2_bus,CiBark2_rail,CiBarkDiff_road,CiBarkDiff_bus,CiBarkDiff_rail,CiBarkPCTDiff_road,CiBarkPCTDiff_bus,CiBarkPCTDiff_rail,CiBar1,CiBar2,CiBarDiff,CiBarPCTDiff,CjBark1_road,CjBark1_bus,CjBark1_rail,CjBark2_road,CjBark2_bus,CjBark2_rail,CjBarkDiff_road,CjBarkDiff_bus,CjBarkDiff_rail,CjBarkPCTDiff_road,CjBarkPCTDiff_bus,CjBarkPCTDiff_rail,CjBar1,CjBar2,CjBarDiff,CjBarPCTDiff,Lik1_road,Lik1_bus,Lik1_rail,Lik2_road,Lik2_bus,Lik2_rail,LikDiff_road,LikDiff_bus,LikDiff_rail,LikPCTDiff_road,LikPCTDiff_bus,LikPCTDiff_rail,Ljk1_road,Ljk1_bus,Ljk1_rail,Ljk2_road,Ljk2_bus,Ljk2_rail,LjkDiff_road,LjkDiff_bus,LjkDiff_rail,LjkPCTDiff_road,LjkPCTDiff_bus,LjkPCTDiff_rail,alphai,alphaj,niPlus_road,niZero_road,niMinus_road,niPlus_bus,niZero_bus,niMinus_bus,niPlus_rail,niZero_rail,niMinus_rail,njPlus_road,njZero_road,njMinus_road,njPlus_bus,njZero_bus,njMinus_bus,njPlus_rail,njZero_rail,njMinus_rail,Rhoi_road,Rhoi_bus,Rhoi_rail,RhoiPlus_road,RhoiPlus_bus,RhoiPlus_rail,RhoiMinus_road,RhoiMinus_bus,RhoiMinus_rail,Rhoj_road,Rhoj_bus,Rhoj_rail,RhojPlus_road,RhojPlus_bus,RhojPlus_rail,RhojMinus_road,RhojMinus_bus,RhojMinus_rail,di_road,di_bus,di_rail,dj_road,dj_bus,dj_rail,deltaihat_road,deltaihat_bus,deltaihat_rail,deltai_road,deltai_bus,deltai_rail,deltaj_road,deltaj_bus,deltaj_rail,deltajhat_road,deltajhat_bus,deltajhat_rail,ti_road,ti_bus,ti_rail,tihat_road,tihat_bus,tihat_rail,tj_road,tj_bus,tj_rail,tjhat_road,tjhat_bus,tjhat_rail,chiik_road,chiik_bus,chiik_rail,chijk_road,chijk_bus,chijk_rail,chii,chij
            //0,E02000001,14337,19958,181116,56102.983997136354,25575.516820713878,133732.49955177307,41765.983997136354,5617.516820713878,-47383.50044822693,139,248,934,833.1533681638539,496.95504786414676,1249.318876415491,694.1533681638539,248.95504786414676,315.3188764154911,215411,215411.0003696233,0.0003696233034133911,1321,2579.4272924434918,1258.4272924434918,8.028912228853489,26.79911611858811,23.99676432744858,5.673864235046554,25.484565767766938,20.42214927428931,-2.3550479938069353,-1.3145503508211718,-3.5746150531592704,-0.2933209289975301,-0.04905200399163119,-0.14896237694306413,58.82479267489018,51.5805792771028,-7.244213397787384,-0.12314898307970802,3.921969531251372,16.925201564065873,16.338500722677885,3.2575698593333184,14.228734059069987,15.6453396220589,-0.6643996719180536,-2.6964675049958853,-0.6931611006189851,-0.16940459802757962,-0.15931671447392345,-0.04242501269757729,37.18567181799513,33.13164354046221,-4.054028277532922,-0.1090212460695969,149869.54542606766,113612.17421167268,2084383.2714453754,402842.6318880137,152843.3796359523,1368428.176311025,252973.08646194602,39231.205424279615,-715955.0951343505,1.6879552529685926,0.34530811241396825,-0.34348533925715363,652.0175579160132,752.9045813597622,4957.923133474425,3052.524178941152,1292.7935033714166,8418.885948666837,2400.506621025139,539.8889220116544,3460.9628151924117,3.6816594766215633,0.7170748264495922,0.6980670579229069,0.26089346558190435,0.04168236878897479,0,983,0,0,983,0,0,983,0,0,983,0,0,983,0,0,983,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,NaN,NaN,NaN,NaN,NaN,NaN,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
            using (StreamReader sr = new StreamReader("C:\\richard\\github\\DataBuilderDiffusion\\QUANT3LSOA\\model-runs\\lonboro_msoa\\ImpactsVector_lonboro_msoa.csv"))
            {
                string[] headers = sr.ReadLine().Split(',');
                foreach (string header in headers)
                {
                    try
                    {
                        dtMSOA.Columns.Add(header);
                    }
                    catch { }
                }
                while (!sr.EndOfStream)
                {
                    string[] rows = sr.ReadLine().Split(',');
                    DataRow dr = dtMSOA.NewRow();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = rows[i];
                    }
                    dtMSOA.Rows.Add(dr);
                }

            }
            //now process the rows - LSOA aggregates int MSOA first
            Dictionary<string, float[]> compTable = new Dictionary<string, float[]>();
            foreach (DataRow row in dtLSOA.Rows)
            {
                string AreaKeyMSOA = row["MSOA11CD"] as string;
                string strDj = row["ImpactsVector_lonboro_lsoa_Cj2"] as string;
                float Dj = Convert.ToSingle(strDj);
                if (compTable.ContainsKey(AreaKeyMSOA))
                {
                    float[] existing = compTable[AreaKeyMSOA];
                    existing[0] += Dj;
                    compTable[AreaKeyMSOA] = existing;
                }
                else
                {
                    compTable.Add(AreaKeyMSOA, new float[] { Dj, 0 });
                }
            }
            //and then process MSOA rows
            foreach (DataRow row in dtMSOA.Rows)
            {
                string AreaKeyMSOA = row["areakey"] as string;
                string strDj = row["Cj2"] as string;
                float Dj = Convert.ToSingle(strDj);
                float[] existing = compTable[AreaKeyMSOA];
                existing[1] = Dj;
                compTable[AreaKeyMSOA] = existing;
            }

            //write out data - LSOAAggregatesToMSOA (0), MSOAModel (1), [0]-[1], PCTDiff=([0]-[1])/[1]*100%
            foreach (KeyValuePair<string, float[]> KVP in compTable)
            {
                float diff = KVP.Value[0] - KVP.Value[1];
                float pct = diff / KVP.Value[1] * 100.0f;
                System.Diagnostics.Debug.WriteLine(KVP.Key + "," + KVP.Value[0] + "," + KVP.Value[1] + "," + diff + "," + pct);
            }
            System.Diagnostics.Debug.WriteLine("END");
        }

        /// <summary>
        /// Returns the geographic extent of the zone centroids in the ZoneCodes file.
        /// NOTE: you probably want to make this a bit bigger to cover the zone areas
        /// as this is just the centroids.
        /// </summary>
        /// <param name="ZoneCodes"></param>
        /// <returns>minLon, maxLon, minLat, maxLat the same as a Envelope</returns>
        public static float[] GetExtentsFromZoneCodes(DataTable ZoneCodes)
        {
            //work out bounds of area here from the zonecodes file
            float minLat = float.MaxValue, maxLat = float.MinValue, minLon = float.MaxValue, maxLon = float.MinValue;
            foreach (DataRow row in ZoneCodes.Rows)
            {
                float lat = (float)row["lat"];
                float lon = (float)row["lon"];
                if (lat < minLat) minLat = lat;
                if (lat > maxLat) maxLat = lat;
                if (lon < minLon) minLon = lon;
                if (lon > maxLon) maxLon = lon;
            }
            return new float[] { minLon, maxLon, minLat, maxLat };
        }

        /// <summary>
        /// Clips a graph to a box defined by the points in the zone codes file.
        /// Expansion factor expands the box by a small amount to make the network
        /// bigger than the box covered by the zone centroid points.
        /// Takes "in" files, clips and writes "out" files.
        /// </summary>
        /// <param name="ZoneCodes"></param>
        /// <param name="ExpansionFactor">E.g. 0.1 degrees for lat lon</param>
        /// <param name="InGraphMLFilename"></param>
        /// <param name="InKDTreeFilename"></param>
        /// <param name="OutGraphMLFilename"></param>
        /// <param name="OutKDTreeFilename"></param>
        public static void ClipNetwork(
            DataTable ZoneCodes,
            float ExpansionFactor,
            string InGraphMLFilename, string InKDTreeFilename,
            string OutGraphMLFilename, string OutKDTreeFilename
        )
        {
            TransportNetwork ptn = new TransportNetwork();
            ptn.DeserialiseGraph(InGraphMLFilename, InKDTreeFilename);
            HashSet<string> nodes = new HashSet<string>();
            float[] zoneextents = DataBuilderLSOA.GetExtentsFromZoneCodes(ZoneCodes);
            Console.WriteLine("Clip Network: xmin=" + zoneextents[0] + " xmax=" + zoneextents[1] + " ymin=" + zoneextents[2] + " ymax=" + zoneextents[3]);
            Envelope env = new Envelope(zoneextents[0], zoneextents[1], zoneextents[2], zoneextents[3]); //query all within area
            env.ExpandBy(ExpansionFactor); //make it a bit bigger - 0.1 of a degree looks about right
            foreach (KdNode<string> node in ptn.kdtree.Query(env))
            {
                //add to a list, then delete the nodes not on the list
                nodes.Add(node.Data);
            }
            //now do the delete by going through all edges and deleting any nodes not on the list
            List<WeightedEdge<string>> deleteEdges = new List<WeightedEdge<string>>();
            foreach (WeightedEdge<string> edge in ptn.graph.Edges)
            {
                //delete the edges where both nodes not on the list - but we have to add to a delete list otherwise the enumeration is modified
                if ((!nodes.Contains(edge.Source)) && (!nodes.Contains(edge.Target)))
                {
                    deleteEdges.Add(edge); //add it to the list for deletion in the next step
                }
            }
            //and finally do the actual delete now we're free of the enumerator
            foreach (WeightedEdge<string> edge in deleteEdges)
            {
                ptn.graph.RemoveEdge(edge);
                //todo: no way to remove from spatial index, so just leave them - or build a new index?
            }

            //now save...
            ptn.SerialiseGraph(OutGraphMLFilename, OutKDTreeFilename);
        }

        /// <summary>
        /// This is the main entry point as it then runs all the various steps to produce the
        /// data based on the modelruns dir, prefix and shapefile.
        /// NOTE: ModelRuns and Prefix and ZonesLookupFilename need to be set in props first.
        /// NOTE: this assumes a working QUANT3 version in ModelRunsDir/EWS/graphs etc.
        /// </summary>
        /// <param name="AreaShapefileName">Full path to shapefile containing zones (LSOA)</param>
        /// <param name="FlowCSVFilename">Full path to csv file containing flows</param>
        public void BuildPrefixDataFromShapefile(
            string AreaShapefileName, string dbAreaKeyAttrName, string dbAreaTextAttrName,
            string FlowCSVFilename)
        {
            PrefixModelRunsDir = Path.Combine(ModelRunsDir, Prefix); //this is what all our files will go into
            Directory.CreateDirectory(PrefixModelRunsDir);
            Directory.CreateDirectory(Path.Combine(PrefixModelRunsDir, "graphs"));
            
            StepZoneCodesCreation(AreaShapefileName, dbAreaKeyAttrName);
            
            //needed for everything else, so we load it in here
            DataTable ZoneCodes = Serialiser.GetXMLDataTable(Path.Combine(PrefixModelRunsDir,ZoneCodesLookupFilename)); //note the QUANT3 method
            ZoneCodes.PrimaryKey = new DataColumn[] { ZoneCodes.Columns["areakey"] };
            
            StepFlowMatrixConstruction(FlowCSVFilename,ZoneCodes);
            StepClipNetworks(ZoneCodes,0.1f); //0.1 is envelope expansion factor
            StepCostMatrixConstruction(ZoneCodes, AreaShapefileName, dbAreaKeyAttrName, dbAreaTextAttrName);
            StepCrowflyMatrixCreation(ZoneCodes);

            //todo: constraints creation?
        }

        /// <summary>
        /// This builds the zonecodes xml file that EVERYTHING needs later
        /// </summary>
        /// <param name="AreaShapefileName"></param>
        /// <param name="dbAreaKeyAttrName">name of attribute containing area code e.g. LSOA11CD</param>
        public void StepZoneCodesCreation(string AreaShapefileName, string dbAreaKeyAttrName)
        {
            Console.WriteLine("Making LSOA zonecodes file");
            //NOTE: this routine filters out duplicate areas and uses its own geo centroid calculation from
            //the polygon geometry to find the lat,lon and east,north coords, so it doesn't use these
            //that are in the file: BNG_E, BNG_N, LONG_, LAT, long, lat_2
            string FullZoneCodesLookupFilename = Path.Combine(PrefixModelRunsDir, ZoneCodesLookupFilename);
            DataTable ZoneCodes;
            if (!File.Exists(FullZoneCodesLookupFilename))
            {
                ZoneCodes = DataBuilder.DeriveAreakeyToZoneCodeFromShapefile(AreaShapefileName, dbAreaKeyAttrName);
                //QUANT2Core.utils.Serialiser.Put(ZoneCodesLookupFilename, ZoneCodes); //NO!
                ZoneCodes.WriteXml(FullZoneCodesLookupFilename);
                ZoneCodes.WriteXmlSchema(Path.ChangeExtension(FullZoneCodesLookupFilename, ".xsd"));
                //DataBuilder.WriteZoneCodesAsCSV(ZoneCodes, ZoneCodesLookupFilename); //NO!
            }
            else
            {
                Console.WriteLine("skipping " + FullZoneCodesLookupFilename + " as file exists");
            }

        }

        /// <summary>
        /// Constructs flow matrices from a census csv file. Fields hard coded into procedure.
        /// </summary>
        /// <param name="FlowCSVFilename"></param>
        /// <param name="ZoneCodes"></param>
        public void StepFlowMatrixConstruction(string FlowCSVFilename, DataTable ZoneCodes)
        {
            //fq files here...
            string FullTObs1Filename = Path.Combine(PrefixModelRunsDir, TObs1Filename);
            string FullTObs2Filename = Path.Combine(PrefixModelRunsDir, TObs2Filename);
            string FullTObs3Filename = Path.Combine(PrefixModelRunsDir, TObs3Filename);


            //now the flow matrices
            //Area of usual residence,	Area Name,	Area of Workplace,	Area of Workplace name,	AllMethods_AllSexes_Age16Plus,	WorkAtHome_AllSexes_Age16Plus,	Underground_AllSexes_Age16Plus,	Train_AllSexes_Age16Plus,	Bus_AllSexes_Age16Plus,	Taxi_AllSexes_Age16Plus,	Motorcycle_AllSexes_Age16Plus,	CarOrVan_AllSexes_Age16Plus,	Passenger_AllSexes_Age16Plus,	Bicycle_AllSexes_Age16Plus,	OnFoot_AllSexes_Age16Plus,	OtherMethod_AllSexes_Age16Plus
            //E01004842,	Bolton 012C,	E01000001,	City of London 001A,	1,	0,	0,	1,	0,	0,	0,	0,	0,	0,	0,	0
            //E01004868,	Bolton 015A,	E01000001,	City of London 001A,	1,	0,	0,	0,	1,	0,	0,	0,	0,	0,	0,	0

            //Or for MSOA
            //"Area of residence","Area of workplace", "All categories: Method of travel to work","Work mainly at or from home","Underground, metro, light rail, tram","Train","Bus, minibus or coach","Taxi","Motorcycle, scooter or moped","Driving a car or van","Passenger in a car or van","Bicycle","On foot","Other method of travel to work"
            //E02000001,E02000001,1506,0,73,41,32,9,1,8,1,33,1304,4
            //E02000002,E02000001,74,0,37,22,6,0,1,7,0,0,1,0

            //ROAD
            if (!File.Exists(FullTObs1Filename))
            {
                //LSOA
                FMatrix TObs1 = DataBuilder.GenerateTripsMatrix(
                    FlowCSVFilename,
                    ZoneCodes,
                    "Area of usual residence",
                    "Area of Workplace",
                    new string[] {
                        "Taxi_AllSexes_Age16Plus",
                        "Motorcycle_AllSexes_Age16Plus",
                        "CarOrVan_AllSexes_Age16Plus",
                        "Passenger_AllSexes_Age16Plus"
                    }
                );
                //MSOA
                //FMatrix TObs1 = DataBuilder.GenerateTripsMatrix(
                //    FlowCSVFilename,
                //    ZoneCodes,
                //    "Area of residence",
                //    "Area of workplace",
                //    new string[] {
                //        "Taxi",
                //        "Motorcycle, scooter or moped",
                //        "Driving a car or van",
                //        "Passenger in a car or van"
                //    }
                //);

                TObs1.DirtySerialise(FullTObs1Filename);
                Console.WriteLine("Written file: " + FullTObs1Filename);
            }
            else
            {
                Console.WriteLine("Skipping file " + FullTObs1Filename + " as file exists");
            }
            //BUS
            if (!File.Exists(FullTObs2Filename))
            {
                //LSOA
                FMatrix TObs2 = DataBuilder.GenerateTripsMatrix(
                    FlowCSVFilename,
                    ZoneCodes,
                    "Area of usual residence",
                    "Area of Workplace",
                    new string[] {
                        "Bus_AllSexes_Age16Plus"
                    }
                );
                //MSOA
                //FMatrix TObs2 = DataBuilder.GenerateTripsMatrix(
                //    FlowCSVFilename,
                //    ZoneCodes,
                //    "Area of residence",
                //    "Area of workplace",
                //    new string[] {
                //        "Bus, minibus or coach"
                //    }
                //);
                TObs2.DirtySerialise(FullTObs2Filename);
                Console.WriteLine("Written file: " + FullTObs2Filename);
            }
            else
            {
                Console.WriteLine("Skipping file " + FullTObs2Filename + " as file exists");
            }
            //RAIL
            if (!File.Exists(FullTObs3Filename))
            {
                FMatrix TObs3 = DataBuilder.GenerateTripsMatrix(
                    FlowCSVFilename,
                    ZoneCodes,
                    "Area of usual residence",
                    "Area of Workplace",
                    new string[] {
                        "Underground_AllSexes_Age16Plus",
                        "Train_AllSexes_Age16Plus"
                    }
                );
                //FMatrix TObs3 = DataBuilder.GenerateTripsMatrix(
                //    FlowCSVFilename,
                //    ZoneCodes,
                //    "Area of residence",
                //    "Area of workplace",
                //    new string[] {
                //        "Underground, metro, light rail, tram",
                //        "Train"
                //    }
                //);
                TObs3.DirtySerialise(FullTObs3Filename);
                Console.WriteLine("Written file: " + FullTObs3Filename);
            }
            else
            {
                Console.WriteLine("Skipping file " + FullTObs3Filename + " as file exists");
            }
            //OK, that's the three obs flow matrices done

            Console.WriteLine("Finished writing TObs1, TObs2, TObs3");
        }

        /// <summary>
        /// Copy graphml files and spatial index from ModelRunsDir into Prefix dir under /graphs.
        /// Clip the networks based on making a clip rectangle from the zonecodes point and then
        /// expanding the box a bit to cover a slightly larger area of network.
        /// Graph filenames are HARD CODED here.
        /// ModelRunsDir/EWS/graphs is used as source and PrefixModelRuns/graphs as destination.
        /// NOTE: rail is NOT clipped as the network is small and sparse.
        /// </summary>
        public void StepClipNetworks(DataTable ZoneCodes,float ExpansionFactor)
        {
            //This section cuts the bus and road graphml files into areas surrounding the zonecodes 

            //rail - don't do anything for this, it's small enough to use the whole of EWS, so just copy the file
            string GBRailGraphMLFilename = Path.Combine(ModelRunsDir, "EWS\\graphs\\gbrail.graphml");
            string GBRailKDTreeFilename = Path.Combine(ModelRunsDir, "EWS\\graphs\\gbrail-kdtree.bin");
            string DestGBRailGraphMLFilename = Path.Combine(PrefixModelRunsDir, "graphs\\gbrail.graphml");
            string DestGBRailKDTreeFilename = Path.Combine(PrefixModelRunsDir, "graphs\\gbrail-kdtree.bin");
            if (!File.Exists(DestGBRailGraphMLFilename))
            {
                File.Copy(GBRailGraphMLFilename, DestGBRailGraphMLFilename);
                File.Copy(GBRailKDTreeFilename, DestGBRailKDTreeFilename);
                Console.WriteLine("Copied files " + DestGBRailGraphMLFilename + " and " + DestGBRailKDTreeFilename);
            }
            else
            {
                Console.WriteLine("Files " + DestGBRailGraphMLFilename + " and " + DestGBRailKDTreeFilename + " exist, skipping");
            }

            //bus
            //graph clip step - this puts clipped network files into a model runs sub directory of graphs
            string BusGraphMLFilename = Path.Combine(ModelRunsDir, "EWS\\graphs\\bus.graphml");
            string BusKDTreeFilename = Path.Combine(ModelRunsDir, "EWS\\graphs\\bus-kdtree.bin");
            string DestBusGraphMLFilename = Path.Combine(PrefixModelRunsDir, "graphs\\bus.graphml");
            string DestBusKDTreeFilename = Path.Combine(PrefixModelRunsDir, "graphs\\bus-kdtree.bin");
            if (!File.Exists(DestBusGraphMLFilename))
            {
                DataBuilderLSOA.ClipNetwork(
                    ZoneCodes,
                    ExpansionFactor,
                    BusGraphMLFilename, BusKDTreeFilename, //IN
                    DestBusGraphMLFilename, DestBusKDTreeFilename //OUT
                );
            }
            else
            {
                Console.WriteLine("Skipping file " + DestBusGraphMLFilename + " as file already exists");
            }

            //road
            //graph clip step - this puts clipped network files into a model runs sub directory of graphs
            string RoadGraphMLFilename = Path.Combine(ModelRunsDir, "EWS\\graphs\\road.graphml");
            string RoadKDTreeFilename = Path.Combine(ModelRunsDir, "EWS\\graphs\\road-kdtree.bin");
            string DestRoadGraphMLFilename = Path.Combine(PrefixModelRunsDir, "graphs\\road.graphml");
            string DestRoadKDTreeFilename = Path.Combine(PrefixModelRunsDir, "graphs\\road-kdtree.bin");
            if (!File.Exists(DestRoadGraphMLFilename))
            {
                DataBuilderLSOA.ClipNetwork(
                    ZoneCodes,
                    ExpansionFactor,
                    RoadGraphMLFilename, RoadKDTreeFilename, //IN
                    DestRoadGraphMLFilename, DestRoadKDTreeFilename //OUT
                );
            }
            else
            {
                Console.WriteLine("Skipping file " + DestRoadGraphMLFilename + " as file already exists");
            }
        }

        public void StepCostMatrixConstruction(
            DataTable ZoneCodes, string AreaShapefileName_WGS84,
            string dbAreaKeyAttrName, string dbAreaTextAttrName)
        {
            //string EnglandWalesScotlandIZMSOA_WGS84 = "../../../data/ZonesBirmingham_WGS84.shp";
            //string dbAreaKeyAttrName = "LSOA11CD";
            //string dbAreaTextAttrName = "LSOA11NM";

            //rail - note, I just copied the ews ones into the bham graphs dir as a hack
            CostMatrices.GenerateGBRailMatrixSecs(PrefixModelRunsDir, ZoneCodes, AreaShapefileName_WGS84, dbAreaKeyAttrName);
            //now do a quality control step on the difficult ones
            //TODO: run the problem count on the current matrix first and only run the following code if you don't get zero problems
            //TODO: note the 8436 cutoff is now *2!
            //FMatrix QCdisGBRailSec = CostMatrices.QualityControlMatrix(
            //    1966, //1645=bham, //3301=livman, //6246=lon, //8436=ews //lonboro_lsoa=4835*2=9670 //lonboro_msoa=983*2=1966
            //    PrefixModelRunsDir, "diagnostic_QC_gbrail.csv", "dis_gbrail_interzone_sec.bin", "dis_gbrail_intrazone_sec.bin", "graphs/gbrailcentroidlookup.bin", "graphs/gbrail.graphml", "graphs/gbrail-kdtree.bin",
            //    ZoneCodes, AreaShapefileName_WGS84, dbAreaKeyAttrName);
            //FMatrix QCdisGBRailMin = DataBuilder.MatrixMultiply(QCdisGBRailSec, 1 / 60.0f);
            //QCdisGBRailSec.DirtySerialise(Path.Combine(PrefixModelRunsDir, "dis_gbrail_sec.bin"));
            //QCdisGBRailMin.DirtySerialise(Path.Combine(PrefixModelRunsDir, "dis_gbrail_min.bin"));

            //CostMatrices.GenerateBusMatrixSecs(false, PrefixModelRunsDir, ZoneCodes, AreaShapefileName_WGS84, dbAreaKeyAttrName, dbAreaTextAttrName);
            ////now do a quality control step on the difficult ones
            ////TODO: note the 8436 cutoff is now *2!
            //FMatrix QCdisBusSec = CostMatrices.QualityControlMatrix(
            //    1966, //1645=bham (had to use=60), //3301=livman, //6246=lon, //8436=ews //lonboro_lsoa=4835*2=9670 //lonboro_msoa=983*2=1966
            //    PrefixModelRunsDir, "diagnostic_QC_bus.csv", "dis_bus_interzone_sec.bin", "dis_bus_intrazone_sec.bin", "graphs/buscentroidlookup.bin", "graphs/bus.graphml", "graphs/bus-kdtree.bin",
            //    ZoneCodes, AreaShapefileName_WGS84, dbAreaKeyAttrName);
            //FMatrix QCdisBusMin = DataBuilder.MatrixMultiply(QCdisBusSec, 1 / 60.0f);
            //QCdisBusSec.DirtySerialise(Path.Combine(PrefixModelRunsDir, "dis_bus_sec.bin"));
            //QCdisBusMin.DirtySerialise(Path.Combine(PrefixModelRunsDir, "dis_bus_min.bin"));

            //road
            CostMatrices.GenerateRoadNetworkMatrixSecs(false, PrefixModelRunsDir, ZoneCodes, AreaShapefileName_WGS84, dbAreaKeyAttrName, dbAreaTextAttrName);
            //TODO: note the 8436 cutoff is now *2!
            //FMatrix QCdisRoadsSec = CostMatrices.QualityControlMatrix(
            //    9670-4, //1645=bham, //3301=livman, //6246=lon *2=12493?<-was this (2?), //8436=ews //lonboro_lsoa=4835*2=9670 //lonboro_msoa=983*2=1966
            //    PrefixModelRunsDir, "diagnostic_QC_roads.csv", "dis_roads_interzone_sec.bin", "dis_roads_intrazone_sec.bin", "graphs/roadcentroidlookup.bin", "graphs/road.graphml", "graphs/road-kdtree.bin",
            //    ZoneCodes, AreaShapefileName_WGS84, dbAreaKeyAttrName);
            //FMatrix QCdisRoadsMin = DataBuilder.MatrixMultiply(QCdisRoadsSec, 1 / 60.0f);
            //QCdisRoadsSec.DirtySerialise(Path.Combine(PrefixModelRunsDir, "dis_roads_sec.bin"));
            //QCdisRoadsMin.DirtySerialise(Path.Combine(PrefixModelRunsDir, "dis_roads_min.bin"));


            //Generate a final problem report for the cost matrices to make sure everthing is fine
            Console.WriteLine("********************************************************************************");
            CostMatrices.GenerateCostMatrixProblemReport(Path.Combine(PrefixModelRunsDir, "dis_gbrail_min.bin"));
            CostMatrices.GenerateCostMatrixProblemReport(Path.Combine(PrefixModelRunsDir, "dis_bus_min.bin"));
            CostMatrices.GenerateCostMatrixProblemReport(Path.Combine(PrefixModelRunsDir, "dis_roads_min.bin"));
            Console.WriteLine("********************************************************************************");

            //for the scenario creation, we need null versions of the APSP files, built directly from the network graphs
            CostMatrices.GenerateNullMatricesMins(
                false, //isGPU
                PrefixModelRunsDir, ZoneCodes,
                new string[] { "graphs/road_QC.graphml", "graphs/bus_QC.graphml", "graphs/gbrail_QC.graphml" },
                new string[] { "graphs/road-kdtree_QC.bin", "graphs/bus-kdtree_QC.bin", "graphs/gbrail-kdtree_QC.bin" },
                new string[] { "graphs/roadcentroidlookup_QC.bin", "graphs/buscentroidlookup_QC.bin", "graphs/gbrailcentroidlookup_QC.bin" },
                new string[] { "dis_roads_sec.bin", "dis_bus_sec.bin", "dis_gbrail_sec.bin" },
                new string[] { "null_dis_roads_min.bin", "null_dis_bus_min.bin", "null_dis_gbrail_min.bin" }
            );
        }

        /// <summary>
        /// Build a crowfly distance matrix using the QC file containing the graph vertex locations
        /// in WGS84, along with the zonecodes.
        /// e.g. gbrailcentroidlookup_QC.csv looks like this (which we get as a datatable)
        /// zonecode,zonei,zone_lat,zone_lon,vertexid,vertex_lat,vertex_lon,distMetres
        /// E01000672,657,51.414608,0.0021129511,DRIVE_E01000672,51.414608001708984,0.0021129511296749115,0
        /// E01000671,656,51.39579,0.025303531,DRIVE_E01000671,51.395790100097656,0.02530353143811226,0
        /// Then it's just a case of doing a great circle distance calculation on the WGS84 coords.
        /// </summary>
        /// <param name="ZoneCodes">Zone codes datatable which just gives us a guaranteed list of zones</param>
        /// <param name="CentroidsDT">Centroids QC file from the data creation step. As shown above.</param>
        /// <returns></returns>
        public FMatrix GenerateVertexCrowflyMatrix(DataTable ZoneCodes, DataTable CentroidsDT)
        {
            int N = ZoneCodes.Rows.Count;
            FMatrix vertexdis = new FMatrix(N,N);
            
            //make a lookup of zonei to zonecode for speed
            Dictionary<int,float> ZoneIAreaLookup = new Dictionary<int,float>();
            foreach (DataRow row in ZoneCodes.Rows)
            {
                //string zonecode = (string)row["zonecode"];
                int zonei = (int)row["zonei"];
                float area = (float)row["area"];
                ZoneIAreaLookup.Add(zonei,area);
            }

            //make a lookup of zonei to centroid points which we can use for speed
            Dictionary<int, float[]> centroids = new Dictionary<int, float[]>();
            foreach (DataRow row in CentroidsDT.Rows)
            {
                //string zonecode = (string)row["zonecode"];
                int zonei = Convert.ToInt32((float)row["zonei"]); //it's loaded as a float as has no type
                float lon = (float)row["vertex_lon"];
                float lat = (float)row["vertex_lat"];
                centroids.Add(zonei,new float[]{lon,lat});
            }

            //OK, now let's do the actual work
            for (int i=0; i<N; i++)
            {
                float [] LL_i = centroids[i];
                for (int j=0; j<N; j++)
                {
                    float[] LL_j = centroids[j];
                    if (i==j)
                    {
                        //todo: intrazone calculation - needs zone area
                        float area = ZoneIAreaLookup[i];
                        //what is the calculation?
                        float dist = (float)Math.Sqrt(area / (2 * Math.PI));
                        vertexdis._M[i, i] = dist / 1000; //todo: depends on area units
                    }
                    else
                    {
                        //interzone calculation
                        float dist = TransportNetwork.GreatCircleDistance(
                            LL_i[1], LL_i[0],
                            LL_j[1], LL_j[0]
                        );
                        vertexdis._M[i, j] = dist / 1000.0f;
                    }
                }
            }
            return vertexdis;
        }

        /// <summary>
        /// Generate a crowfly matrix of distance between zone centroids in KM.
        /// NOTE: this was at the top of the cost matrix creation in the databuilder2 - moved as it doesn't require shortest paths.
        /// How on earth were the vertex crowfly KM matrices built in the original QUANT? Rewritten from scratch here.
        /// </summary>
        /// <param name="ZoneCodes"></param>
        public void StepCrowflyMatrixCreation(DataTable ZoneCodes)
        {
            //generate a crowfly matrix of distances which we use for quality control
            string DisCrowflyFilename = Path.Combine(PrefixModelRunsDir, "dis_crowfly_KM.bin");  //what was this? why? "dis_crowfly_vertex_KM.bin"
            if (File.Exists(DisCrowflyFilename))
                Console.WriteLine("CostMatrices:: " + DisCrowflyFilename + " exists, skipping");
            else
            {
                Console.WriteLine("CostMatrices:: creating " + DisCrowflyFilename);
                FMatrix dis_crowfly = CostMatrices.GenerateCrowflyMatrix(ZoneCodes);
                dis_crowfly.DirtySerialise(DisCrowflyFilename);
            }

            //now create the vertex KM files - dis_crowfly_vertex_roads_KM.bin, dis_crowfly_vertex_bus_KM.bin and dis_crowfly_vertex_gbrail_KM.bin
            string[] modeName = { "roads", "bus", "gbrail" }; //vertex dis_xxx filenames
            string[] centroidModeName = { "road", "bus", "gbrail" }; //QC graph files, annoyingly different!
            for (int k=0; k<3; k++)
            {
                string VertexFilename = Path.Combine(PrefixModelRunsDir,"dis_crowfly_vertex_" + modeName[k] + "_KM.bin");
                if (File.Exists(VertexFilename))
                {
                    Console.WriteLine("CostMatrices:: " + VertexFilename + " exists, skipping");
                }
                else
                {
                    Console.WriteLine("CostMatrices:: creating " + VertexFilename);
                    string CentroidsFilename = Path.Combine(PrefixModelRunsDir, "graphs/"+centroidModeName[k] +"centroidlookup_QC.csv");
                    DataTable CentroidsDT = Serialiser.LoadCSV(CentroidsFilename);
                    FMatrix dis_vertexcrowfly = GenerateVertexCrowflyMatrix(ZoneCodes,CentroidsDT);
                    dis_vertexcrowfly.DirtySerialise(VertexFilename);
                }
            }
        }


    }

}
