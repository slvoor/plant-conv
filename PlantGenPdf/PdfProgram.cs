using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using System.Data.OleDb;
using System.IO;

namespace PlantGenPdf
{
    class PdfProgram
    {
        class Gebied
        {
            public int x1, y1, x2, y2;
        }
        class Soort
        {
            public string naamN, naamW;
        }

        class Voorkomen
        {
            public string srn;
            public Soort soort;
            public DateTime eerst, laatst;
            public int aantal;
        }

        private Dictionary<string, string> soortVervanging = new Dictionary<string, string>();
        private Dictionary<string, Soort> soorten = new Dictionary<string, Soort>();
        private Dictionary<string, Gebied> gebieden = new Dictionary<string, Gebied>();

        void LaadGebieden(OleDbConnection conn)
        {
            OleDbCommand cmd = new OleDbCommand("Select SP_CODE,xRDf,yRDf FROM gebieden", conn);
            OleDbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string spCode = (string)reader.GetValue(0);
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


        void LaadSoorten(OleDbConnection conn)
        {
            soortVervanging.Clear();
            soorten.Clear();
            OleDbCommand cmd = new OleDbCommand("SELECT Snr, Snrvv,NaamN,NaamW FROM namen;", conn);
            OleDbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string soortNr = (string)reader.GetValue(0);
                string soortVervang = (string)reader.GetValue(1);
                object naamN = reader.GetValue(2);
                object naamW = reader.GetValue(3);
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
                if (soortVervanging.TryGetValue(soortNr, out newSoortNr))
                {
                    soortNr = newSoortNr;
                }
                else break;
            }
            return soortNr;
        }

        SortedDictionary<string, Voorkomen> BepaalVoorkomens(OleDbConnection conn, int kmx, int kmy)
        {
            // key is used for sorting
            SortedDictionary<string, Voorkomen> voorkomens = new SortedDictionary<string, Voorkomen>();
            int cnt = 0;
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
                if (soorten.TryGetValue(soortNr, out s) == false)
                {
                    Console.WriteLine("onbekende soort {0}", soortNr);
                }

                string spCode = (string)reader.GetValue(1);
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

                int scale = 1000;
                if (g.x1 >= kmx * scale && g.x2 <= (kmx+1) * scale
                && g.y1 >= kmy * scale && g.y2 <= (kmy+1) * scale)
                {
                    // inside
                    ; // OK
                }
                else
                {
                    continue; // skip
                }

                DateTime d1 = (DateTime)reader.GetValue(2);
                DateTime d2 = (DateTime)reader.GetValue(3);

                int soortNummer = int.Parse(soortNr);

                string sleutel = s.naamN;

                Voorkomen voorkomen;
                if (voorkomens.TryGetValue(sleutel,out voorkomen)==false)
                {
                    voorkomen = new Voorkomen();
                    voorkomen.soort = s;
                    voorkomen.srn = soortNr;
                    voorkomen.eerst = d1;
                    voorkomen.laatst = d2;
                    voorkomens.Add(sleutel, voorkomen);
                } else
                {
                    if (d1 < voorkomen.eerst) voorkomen.eerst = d1;
                    if (d2 > voorkomen.laatst) voorkomen.laatst = d2;
                }
                voorkomen.aantal++;
            }
            return voorkomens;
        }

        static void Main(string[] args)
        {
            PdfProgram p = new PdfProgram();
            int ret = p.Run(args);
            if (ret == 1)
            {
                Console.WriteLine("Needed parameters not specified");
                Console.WriteLine("--in database.mdb --out output.pdf");
            }
            Console.WriteLine("End of program");
            //Console.ReadLine();
        }
        int Run(string[] args)
        {
            string srcdbfile = "";
            string outputfile = "";
            string param = "";
            foreach (string arg in args)
            {
                if (param == "--in") srcdbfile = arg;
                if (param == "--out") outputfile = arg;
                param = "";
                if (arg == "--in") param = arg;
                if (arg == "--out") param = arg;
            }

            if (srcdbfile == "") return 1;
            if (outputfile == "") return 1;

            string connectionString = "Provider=Microsoft.Jet.OLEDB.4.0"
                + ";Data Source=" + srcdbfile;
            OleDbConnection conn = new OleDbConnection(connectionString);
            conn.Open();

            LaadSoorten(conn);
            LaadGebieden(conn);

            int kmx = 94, kmy = 453;
            var voorkomens = BepaalVoorkomens(conn,kmx,kmy);

            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(1.1f, Unit.Centimetre);
                    page.Content()
                        .DefaultTextStyle(style => style.FontSize(10) )
                        .Column(col =>
                        {
                            col.Item()
                            .Table(table =>
                            {
                                //table.
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(10);
                                    cols.RelativeColumn(40);
                                    cols.RelativeColumn(40);
                                    cols.RelativeColumn(10);
                                    cols.RelativeColumn(10);
                                    cols.RelativeColumn(10);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(StyleTH).Text("SNR");
                                    header.Cell().Element(StyleTH).Text("Nederlandse naam");
                                    header.Cell().Element(StyleTH).Text("Wetenschappelijke naam");
                                    header.Cell().Element(StyleTH).Text("Eerst");
                                    header.Cell().Element(StyleTH).Text("Laatst");
                                    header.Cell().Element(StyleTH).Text("#WRN");

                                });

                                uint rowix = 1;

                                foreach (var kv in voorkomens)
                                {
                                    Voorkomen voorkomen = kv.Value;
                                    table.Cell().Row(rowix).Column(1).Element(StyleTD).Text(voorkomen.srn);
                                    table.Cell().Element(StyleTD).Text(voorkomen.soort.naamN);
                                    table.Cell().Element(StyleTD).Text(voorkomen.soort.naamW);
                                    table.Cell().Element(StyleTD).Text(voorkomen.eerst.Year.ToString());
                                    table.Cell().Element(StyleTD).Text(voorkomen.laatst.Year.ToString());
                                    table.Cell().Element(StyleTD).Text(voorkomen.aantal);
                                    rowix++;
                                    Console.WriteLine(kv.Key);
                                }

                                /*
                                table.Cell().Row(1).Column(1).Element(StyleTD).Text("717");
                                table.Cell().Element(StyleTD).Text("Aardaker");
                                table.Cell().Element(StyleTD).Text("Lathyrus tuberosus");
                                table.Cell().Element(StyleTD).Text("2003");
                                table.Cell().Element(StyleTD).Text("2021");
                                table.Cell().Element(StyleTD).Text("3");

                                table.Cell().Row(2).Column(1).Element(StyleTD).Text("529");
                                table.Cell().Element(StyleTD).Text("aardbei, Bos-");
                                table.Cell().Element(StyleTD).Text("Fragaria vesca");
                                table.Cell().Element(StyleTD).Text("2003");
                                table.Cell().Element(StyleTD).Text("2021");
                                table.Cell().Element(StyleTD).Text("3");
                                */



                            });
                        });
                });
            })
            .GeneratePdf(outputfile);

            Console.WriteLine("Generated " + outputfile);

            return 0;
        }

        IContainer StyleTH(IContainer container)
        {
            return container.BorderBottom(1)
                .Background("00aa00");
        }

        IContainer StyleTD(IContainer container)
        {
            return container.BorderBottom(1);
        }

    }
}
