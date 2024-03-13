using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;

namespace PlantGenPdf
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p = new Program();
            int ret = p.Run(args);
            if (ret == 1)
            {
                Console.WriteLine("Needed parameters not specified");
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

                                

                            });
                        });
                });
            })
            .GeneratePdf(outputfile);

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
