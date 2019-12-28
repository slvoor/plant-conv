using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data;
using System.IO;

namespace PlantDbConv
{
    class Gebied
    {
        public int x1, y1, x2, y2;
    }
    class Soort
    {
        public string naamN, naamW;
        public int aantal = 0;
    }

    class Program
    {
        private Dictionary<string, string> soortVervanging = new Dictionary<string, string>();
        private Dictionary<string, Soort> soorten = new Dictionary<string, Soort>();
        private Dictionary<string, Gebied> gebieden = new Dictionary<string, Gebied>();

        static void ShowRunInfo()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("plantdbconv --in db.mdb --out data.sql");
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            int ret = p.Run(args);
            if (ret==1)
            {
                Console.WriteLine("Needed parameters not specified");
                ShowRunInfo();
            }
            Console.WriteLine("End of program");
            Console.ReadLine();
        }

        string SqlTextEscape(string value)
        {
            return "'" + value.Replace("'", "''") + "'";
        }

        string SqlDate(DateTime dt)
        {
            return "'" + dt.ToString("yyyy-MM-dd") + "'";
        }

        string ArraySqlValues(object[] values)
        {
            bool first = true;
            string t = "";
            foreach (object value in values)
            {
                if (first) first = false;
                else t += ",";
                if (value is int) t += value;
                else if (value is string) t += SqlTextEscape((string)value);
                else if (value is DateTime) t += SqlDate((DateTime)value);
                else t += "NULL";
            }
            return t;
        }

        void SchrijfSoorten(StreamWriter writer)
        {
            writer.WriteLine("DELETE FROM Soorten;");
            writer.WriteLine("BEGIN;");
            foreach (KeyValuePair<string,Soort> kv in soorten)
            {
                Soort s = kv.Value;
                if (s.aantal == 0) continue;
                string sql = "INSERT INTO Soorten(id,naamN,naamW) VALUES (";
                sql += ArraySqlValues(new object[] { int.Parse(kv.Key), s.naamN, s.naamW });
                sql += ");";
                writer.WriteLine(sql);
            }
            writer.WriteLine("COMMIT;");
        }

        void LaadSoorten(OleDbConnection conn, StreamWriter writer)
        {
            soortVervanging.Clear();
            soorten.Clear();
            OleDbCommand cmd = new OleDbCommand("SELECT Snr, Snrvv,NaamN,NaamW FROM namen;", conn);
            OleDbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string soortNr      = (string) reader.GetValue(0);
                string soortVervang = (string) reader.GetValue(1);
                object naamN        = reader.GetValue(2);
                object naamW        = reader.GetValue(3);
                if (soortVervang != soortNr)
                {
                    soortVervanging[soortNr] = soortVervang;
                    //Console.WriteLine("Soort {0} => {1}", soortNr, soortVervang);
                    continue;
                }
                if (naamN != DBNull.Value)
                {
                    Soort s = new Soort();
                    s.naamN = (string)naamN;
                    s.naamW = (string)naamW;
                    soorten[soortNr] = s;
                    int soortNummer = int.Parse(soortNr);
                }
            }

        }

        // we staan meerdere vervangingen toe achter elkaar geschakeld
        string DoeSoortVervanging(string soortNr)
        {
            int cnt = 0;
            while (true)
            {
                cnt++;
                // max.10 om eindeloze loop te voorkomen
                if (cnt > 10) throw new Exception("te veel soortvervanging (loop?)");
                string newSoortNr;
                if (soortVervanging.TryGetValue(soortNr,out newSoortNr))
                {
                    soortNr = newSoortNr;
                }
                else break;
            }
            return soortNr;
        }

        void LaadGebieden(OleDbConnection conn)
        {
            OleDbCommand cmd = new OleDbCommand("Select SP_CODE,xRDf,yRDf FROM gebieden", conn);
            OleDbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string spCode = (string) reader.GetValue(0);
                spCode = spCode.Trim().ToLower();
                string x = (string)reader.GetValue(1);
                string y = (string)reader.GetValue(2);
                int x1, x2, y1, y2;
                int factor = 0;
                if (x.Length == 3) factor = 1000;
                if (x.Length == 4) factor = 100;
                if (x.Length == 5) factor = 10;
                if (x.Length == 6) factor = 1;
                if (factor == 0) continue;
                x1 = int.Parse(x) * factor;
                y1 = int.Parse(y) * factor;
                x2 = x1 + factor;
                y2 = y1 + factor;
                Gebied g = new Gebied();
                g.x1 = x1;
                g.x2 = x2;
                g.y1 = y1;
                g.y2 = y2;
                //Console.WriteLine("{0} = {1},{2} => {3},{4} - {5},{6}",spCode,x,y,x1,y1,x2,y2);
                if (gebieden.ContainsKey(spCode))
                {
                    throw new Exception("dubbel gebied");
                }
                gebieden[spCode] = g;
            }
        }

        void LaadWaarnemingen(OleDbConnection conn, StreamWriter writer)
        {
            int cnt = 0;
            writer.WriteLine("DELETE FROM Waarnemingen;");
            writer.WriteLine("BEGIN;");
            HashSet<string> onbekendGebied = new HashSet<string>();
            OleDbCommand cmd = new OleDbCommand("Select SOORT_NR, SP_CODE, Datum1, Datum2 FROM WZTM;", conn);
            OleDbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                cnt++;
                //if (cnt > 100) break;

                string oriSoortNr = (string)reader.GetValue(0);
                string soortNr = DoeSoortVervanging(oriSoortNr);
                /*
                if (soortNr != oriSoortNr)
                {
                    Console.WriteLine("Soort {0} {1} => {2} {3}"
                        , oriSoortNr, soortNamenN[oriSoortNr]
                        , soortNr, soortNamenN[soortNr] );
                    break;
                }
                */

                Soort s;
                if (soorten.TryGetValue(soortNr,out s)==false)
                {
                    Console.WriteLine("onbekende soort {0}", soortNr);
                }
                s.aantal++;

                string spCode = (string) reader.GetValue(1);
                spCode = spCode.Trim().ToLower();

                Gebied g;
                if (gebieden.TryGetValue(spCode, out g) == false)
                {
                    if (onbekendGebied.Add(spCode))
                    {
                        //throw new Exception("onbekend gebied " + spCode);
                        Console.WriteLine("onbekend gebied '{0}'", spCode);
                    }
                    continue;
                }

                DateTime d1 = (DateTime) reader.GetValue(2);
                DateTime d2 = (DateTime)reader.GetValue(3);

                int soortNummer = int.Parse(soortNr);

                //Console.WriteLine("W {0} @ {1},{2} {3}", soortNamenN[oriSoortNr], g.x1, g.y1, d1);
                string sql = "INSERT INTO Waarnemingen(soort,x1,y1,x2,y2,datum1,datum2)"
                    + " VALUES ("
                    + ArraySqlValues(new object[] 
                    { soortNummer
                    , g.x1
                    , g.y1
                    , g.x2
                    , g.y2
                    , d1
                    , d2
                    })
                    + ");";
                writer.WriteLine(sql);
            }
            writer.WriteLine("COMMIT;");
            Console.WriteLine("Waarnemingen aantal = {0}", cnt);
        }

        int Run(string[] args)
        {
            string outputSqlFile = "";
            string srcdbfile = "";

            string param = "";
            foreach (string arg in args)
            {
                if (param == "--in") srcdbfile = arg;
                if (param == "--out") outputSqlFile = arg;
                param = "";
                if (arg == "--in") param = arg;
                if (arg == "--out") param = arg;
            }

            if (outputSqlFile == "") return 1;
            if (srcdbfile == "") return 1;

            StreamWriter output = new StreamWriter(outputSqlFile);
            
            string connectionString = "Provider=Microsoft.Jet.OLEDB.4.0"
                + ";Data Source=" + srcdbfile;
            OleDbConnection conn = new OleDbConnection(connectionString);
            conn.Open();

            LaadSoorten(conn,output);
            LaadGebieden(conn);
            LaadWaarnemingen(conn,output);
            SchrijfSoorten(output);

            conn.Close();
            output.Close();

            return 0;
        }
    }
}
