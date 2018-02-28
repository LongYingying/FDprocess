using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

using System.IO;
using System.Diagnostics;


using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;

using SharpMap;
using SharpMap.CoordinateSystems;
using SharpMap.CoordinateSystems.Transformations;
using ESRI.ArcGIS.Geometry;

namespace FDprocess
{
    class Program
    {

        private Dictionary<int, floatingCar> floCar;
        private static LicenseInitializer m_AOLicenseInitializer = new FDprocess.LicenseInitializer();
        


        static void Main(string[] args)
        {
            m_AOLicenseInitializer.InitializeApplication(new esriLicenseProductCode[] { esriLicenseProductCode.esriLicenseProductCodeEngine },
           new esriLicenseExtensionCode[] { });


            if (!ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.Engine))
            {
                if (!ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.Desktop))
                {
                    Console.WriteLine("Unable to bind to ArcGIS runtime. Application will be shut down.");
                    return;
                }
            }

            m_AOLicenseInitializer.ShutdownApplication();

            
            //SqlServer m_SqlServer = new SqlServer();
            //m_SqlServer.mySqlOpenCon();
            //m_SqlServer.filepath= @"./testFD.txt";
            //m_SqlServer.readSqlData();
            //m_SqlServer.mySqlColseCon();

            Program p = new Program();
            p.LoadBigTxt("testFD.txt", 1);//大文件(文件位置，起始读取行数)


        }

        #region 将经纬度坐标转换为地坐标（米）

        public void BL2XY(double iLongtitude, double iLatitude, out double iProjectedX, out double iProjectedY)
        {
            SharpMap.Geometries.Point ptInput = new SharpMap.Geometries.Point(iLongtitude, iLatitude);
            iProjectedX = -1;
            iProjectedY = -1;

            //if (Math.Abs(ptInput.X) > 360)
            //    return null;

            int nCenterLongitude = ((int)(ptInput.X / 3)) * 3;

            CoordinateSystemFactory cFac = new SharpMap.CoordinateSystems.CoordinateSystemFactory();
            //创建椭球体
            IEllipsoid ellipsoid = cFac.CreateFlattenedSphere("Xian 1980", 6378140, 298.257, SharpMap.CoordinateSystems.LinearUnit.Metre);
            IHorizontalDatum datum = cFac.CreateHorizontalDatum("Xian_1980", DatumType.HD_Geocentric, ellipsoid, null);

            //创建地理坐标系
            SharpMap.CoordinateSystems.IGeographicCoordinateSystem gcs = cFac.CreateGeographicCoordinateSystem(
                     "Xian 1980", SharpMap.CoordinateSystems.AngularUnit.Degrees, datum,
                     SharpMap.CoordinateSystems.PrimeMeridian.Greenwich,
                      new AxisInfo("Lon", AxisOrientationEnum.East),
                      new AxisInfo("Lat", AxisOrientationEnum.North));

            List<ProjectionParameter> parameters = new List<ProjectionParameter>(5);
            parameters.Add(new ProjectionParameter("latitude_of_origin", 0));
            parameters.Add(new ProjectionParameter("central_meridian", nCenterLongitude));
            parameters.Add(new ProjectionParameter("scale_factor", 1.0));
            parameters.Add(new ProjectionParameter("false_easting", 500000));
            parameters.Add(new ProjectionParameter("false_northing", 0.0));

            //创建投影坐标系
            SharpMap.CoordinateSystems.IProjection projection = cFac.CreateProjection("Transverse Mercator", "Transverse_Mercator", parameters);

            SharpMap.CoordinateSystems.IProjectedCoordinateSystem coordsys = cFac.CreateProjectedCoordinateSystem(
                      "Xian_1980_3_Degree_GK_CM", gcs,
                      projection, SharpMap.CoordinateSystems.LinearUnit.Metre,
             new AxisInfo("East", AxisOrientationEnum.East),
             new AxisInfo("North", AxisOrientationEnum.North));

            //创建坐标转换器
            ICoordinateTransformation trans = new CoordinateTransformationFactory().CreateFromCoordinateSystems(gcs, coordsys);
            //工作区坐标到投影坐标系的转换
            SharpMap.Geometries.Point ptOutput = trans.MathTransform.Transform(ptInput);
            iProjectedX = ptOutput.X;
            iProjectedY = ptOutput.Y;
        }

        #endregion

        private bool LoadBigTxt(string rawTxt, int startLine) //读取浮动车txt中的记录至
        {
            
            Stopwatch st = new Stopwatch();
            Console.WriteLine("读取文件中...");
            string m_rawTxt = rawTxt;
            int m_startLine = startLine;
            floCar = new Dictionary<int, floatingCar>();
            int count = 0;//记录个数
            int readline = 0;//从第二行开始读文件,第一行为表头

            int NowStatus;//该辆车当前载客状况
            int breakline = 0;//当part的长度不等于9时，跳出循环


            //写文件
            var fw = File.Open("Result.txt", FileMode.Create);
            //bufferSearch.SetPathAndBuffer("ID", @"E:\data\新建文件夹\数据\医院.shp", @"E:\团梦困\e\Project\DMJProject\Shenzhen Accessibility\BaseData\Voronoi\SZ_StationVoronoi_Xian1980.shp", 500);
            //bufferSearch.LoadBaseRTree("ID", @"E:\团梦困\e\Project\DMJProject\Shenzhen Accessibility\BaseData\Voronoi\SZ_StationVoronoi_Xian1980.shp");
            //bufferSearch.LoadHospitalRTree("ID", @"E:\data\新建文件夹\数据\医院.shp");
            var sw = new StreamWriter(fw);
            var t = 0l;
            Dictionary<int, int> hos = new Dictionary<int, int>();
            Dictionary<int, int> station = new Dictionary<int, int>();
            string temp = "";
            if (File.Exists(m_rawTxt))
            {
                StreamReader srNow = new StreamReader(File.Open(m_rawTxt, FileMode.Open));
                while (true)
                {
                    st.Restart();
                    string line = srNow.ReadLine();

                    if (line == string.Empty || line == null)
                    {
                        break;
                    }
                    
                    string[] part = line.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);//读取txt到数组
                    if (part.Length < 5)
                    {
                        breakline = readline;
                        Console.WriteLine(line);
                    }
                    if (readline >= m_startLine && readline != breakline)
                    {
                        int FID = count;
                        int carid = Convert.ToInt32(part[0]);
                        Decimal UTCtime= Convert.ToDecimal(part[1]);
                        double corX = Convert.ToDouble(part[2]);
                        double corY = Convert.ToDouble(part[3]);
                        int status = Convert.ToInt32(part[4]);

                        NowStatus = status;

                        if (NowStatus == 1)
                        {

                            floatingCar s = new floatingCar();

                            s.CarID = carid;
                            s.Org_UTCtime = UTCtime;
                            s.OrgX = corX;
                            s.OrgY = corY;
                            
                            if (!floCar.ContainsKey(count))
                                floCar.Add(count, s);
                            string[] part1 = line.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                            if (part1.Length != 5)
                            {
                                continue;
                            }

                            while (Convert.ToInt32(part1[4]) != 0)
                            {
                                line = srNow.ReadLine();
                                if (line == string.Empty || line == null)
                                {
                                    break;
                                }
                                part1 = line.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                                if (part1.Length != 5)
                                {
                                    continue;
                                }

                            }
                            
                            if (Convert.ToInt32(part1[0]) == carid)
                            {
                                floCar[count].DesX = Convert.ToDouble(part1[2]);
                                floCar[count].DesY = Convert.ToDouble(part1[3]);
                                floCar[count].Des_UTCtime = Convert.ToDecimal(part1[1]);

                                sw.WriteLine(floCar[count].CarID + "," +floCar[count].Org_UTCtime + "," + floCar[count].OrgX + "," + floCar[count].OrgY
                                            + "," + floCar[count].Des_UTCtime + "," + floCar[count].DesX + "," + floCar[count].DesY + ","
                                                   );                                   
                                    count++;
                            }
                            

                        }


                    }
                    st.Stop();

                    t += st.ElapsedMilliseconds;
                    if (readline % 1000 == 0)
                    {
                       // sw.Write(temp);
                       // sw.Flush();
                        Console.WriteLine("读到" + readline + "行" + count + ",平均每1000行时间:" + (t));
                        t = 0L;
                        temp = "";
                    }
                    readline++;
                }

                srNow.Close();
                sw.Close();
                fw.Close();
            }
            else
                Console.WriteLine("未找到记录文件" + m_rawTxt + "错误");
            Console.WriteLine("读取完毕");
            return true;
        }




    }
   
        public class floatingCar
    {
        public int FID { get; set; } //
        public int CarID { get; set; }//浮动车ID
      
        public  decimal Org_UTCtime { get; set; }//起始时间
        public double OrgX { get; set; } //起点X坐标
        public double OrgY { get; set; } //起点Y坐标
        public decimal Des_UTCtime { get; set; }//终止时间
        public double DesX { get; set; } //终点X坐标
        public double DesY { get; set; } //终点Y坐标

        public int staID { get; set; }//基站ID
    }

        public class carStatus
{
    public int FID { get; set; } //
    public int CarID { get; set; }//浮动车ID
    public int LastStatus { get; set; }//该辆车上次载客状况
    public int NowStatus { get; set; }//该辆车当前载客状况
}
}
