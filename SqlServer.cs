using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Data.SqlClient;
namespace FDprocess
{
    class SqlServer
    {
         SqlConnection con=null;
         string m_server;
         string m_database;
         string m_uid;
         string m_pwd;
         public string filepath;

       public SqlServer()
        {
            con = new SqlConnection();
        }
        public void InitialData()
        {
            m_server = "******";
            m_database = "******";
            m_uid = "**";
            m_pwd = "**";
        }
        //连接数据库
        public  void mySqlOpenCon()
        {
            //con.ConnectionString = "server=.;database=test;integrated security = SSPI"; //windows身份验证

            //sqlserver身份验证
            InitialData();
            string conStr = string.Format("server={0};database={1};uid={2};pwd={3}",m_server,m_database,m_uid,m_pwd);
            con.ConnectionString = conStr;  

            try
            {
                if (con.State == System.Data.ConnectionState.Closed)
                {
                    con.Open();
                    Console.WriteLine("成功连接数据库");

                }
                
            }
            catch(Exception e)
            {
                Console.WriteLine("连接数据库失败！");
                Console.WriteLine(e.Message);
                throw;
            }
            
        }

        public void readSqlData()
        {
            try
            {
                //string filepath = @"./testFD.txt";
                FileStream fs = null;
                StreamWriter sw=null;
                SqlCommand cmd = null;
                SqlDataReader reader = null;
                //写入文件
                     fs= File.Open(filepath, FileMode.Create);
                     sw= new StreamWriter(fs);
                
                
                cmd = con.CreateCommand();
                cmd.CommandText = "select VehicleID,UtcTime,mmX,mmY,TemValue ,row_number() over(partition by VehicleID order by UtcTime) as rum  from MatchedTraj20090901";
                cmd.CommandTimeout = 90;//延长超时时间.

                reader = cmd.ExecuteReader();
                Console.WriteLine("读取文件中。。。");
                sw.WriteLine("VehicleID,UtcTime,OX,OY,status");
                while (reader.Read())
                {
                   
                    int VehicleID = reader.GetInt32(reader.GetOrdinal("VehicleID"));
                    Decimal UtcTime = reader.GetDecimal(reader.GetOrdinal("UtcTime"));
                    double OX = reader.GetDouble(reader.GetOrdinal("mmX"));
                    double OY = reader.GetDouble(reader.GetOrdinal("mmY"));
                    
                    int status = reader.GetInt32(reader.GetOrdinal("TemValue"));
                    // Console.WriteLine(reader.GetDecimal(reader.GetOrdinal("UtcTime")));
                    // Console.WriteLine(reader.GetInt32(reader.GetOrdinal("VehicleID")));
                    //Console.WriteLine(reader.GetInt32(reader.GetOrdinal("TemValue")));

                    sw.WriteLine(VehicleID+","+ UtcTime + ","+ OX + ","+ OY + ","+ status);
                    //double DX, DY;
                    //if(status==1)
                    //{
                    //    sw.WriteLine(VehicleID+","+ UtcTime + "," + OX + "," + OY + ",");
                    //}
                    //while (reader.Read())
                    //{
                    //    Console.WriteLine(reader.GetInt32(reader.GetOrdinal("VehicleID"))+","+ reader.GetInt32(reader.GetOrdinal("TemValue"))+",");
                    //    //Console.WriteLine(+reader.GetInt32(reader.GetOrdinal("rum")));
                    //    if (reader.GetInt32(reader.GetOrdinal("TemValue")) != 0)
                    //    {
                           
                    //        continue;
                          
                    //    }
                    //    else
                    //        break;
                    //}

                    //if (reader.GetInt32(reader.GetOrdinal("VehicleID")) == VehicleID)
                    //{
                    //    DX = reader.GetDouble(reader.GetOrdinal("mmX"));
                    //    DY = reader.GetDouble(reader.GetOrdinal("mmY"));
                    //    sw.Write(DX + "," + DY);
                    //}


                    
                }

                reader.Close();
                sw.Close();
                fs.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                
                con.Close();
                throw;
            }
        }
        //关闭数据库连接
        public void mySqlColseCon()
        {
            try
            {
                if (con.State == System.Data.ConnectionState.Open)
                {
                    con.Close();
                    Console.WriteLine("成功关闭数据库连接");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("关闭数据库失败！");
                Console.WriteLine(e.Message);
                throw;
            }
        }
    }
}
