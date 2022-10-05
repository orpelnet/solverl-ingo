using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GrupoDNC.Solver.Lingo.API
{
    public class LingoCall
    {
        public ResultLingo result { get; set; }

        public string LingoAPI(FileInfo fileModel, int tipo)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory.ToString() + "Lingo";
            string fileLog = string.Format("Solver_{0}.log", Guid.NewGuid().ToString());
            string filePath = path + "\\" + fileLog;

            DateTime diaHoje = new DateTime();
            diaHoje = DateTime.Now;

            var diaFile = diaHoje.ToShortDateString().Replace("/", "");

            string filePathModel = fileModel.ToString();
            string filePathBackup = path + "\\Erro_" + diaFile + ".lng";

            string filePathBackupOK = path + "\\Ok_" + diaFile + ".lng";

            LingoSolver LINGOSolver = new LingoSolver();
            result = LINGOSolver.Solver(fileModel, fileLog);

            if (result.DStatus != 0)
            {
                using (var reader = new StreamReader(filePathModel))
                using (var writer = new StreamWriter(filePathBackup, append: false))
                {
                    writer.Write(reader.ReadToEnd());
                }
            }
            else
            {
                using (var reader = new StreamReader(filePathModel))
                using (var writer = new StreamWriter(filePathBackupOK, append: false))
                {
                    writer.Write(reader.ReadToEnd());
                }
            }

            try
            {
                DataTable SolverLingoDB = new DataTable("SolverLingoDB");
                SolverLingoDB.Columns.Add("Variable", typeof(string));
                SolverLingoDB.Columns.Add("Value", typeof(decimal));
                SolverLingoDB.Columns.Add("ReducedCost", typeof(double));
                SolverLingoDB.Columns.Add("Slack", typeof(decimal));

                using (StreamReader sr = new StreamReader(filePath))
                {
                    int i = 0;
                    string palavra = "Variable";
                    string palavra2 = "Z";
                    bool addLinha = false;
                    string linha;
                    while ((linha = sr.ReadLine()) != null)
                    {
                        if (linha.Contains(palavra) || addLinha)
                        {
                            addLinha = true;
                            if (addLinha)
                            {
                                i++;
                                string[] lines = Regex.Split(linha.TrimStart().Replace("\"", ""), "        ");
                                foreach (string line in lines)
                                {
                                    if (lines.Length == 3)
                                    {
                                        if (i == 1 && lines[0] != palavra)
                                        {
                                            if (linha.Contains(palavra2)) goto Sai;
                                            SolverLingoDB.Rows.Add(new object[]{
                                                lines[0],
                                                lines[1].Replace(".",","),
                                                lines[2].Replace(".",","),
                                                GetSlak(lines[0].ToString(), filePath, tipo)
                                            });
                                            i = 0;
                                        }
                                    }
                                }
                            }
                            i = 0;
                        }
                    }
                }

                Sai:
                fileModel.Delete();
                var log = new FileInfo(path + "\\" + fileLog);
                log.Delete();
                //return JsonConvert.SerializeObject(SolverLingoDB);
                return null;

            }
            catch
            {
                throw;
            }
        }

        private decimal GetSlak(string param, string file, int tipo)
        {
            if (tipo == 1)
                return 0;

            var valores = File.ReadAllLines(file)
                  .Where(l => l.TrimStart().StartsWith("U_ST_" + param))
                  .FirstOrDefault();
            string[] lines = Regex.Split(valores.ToString().TrimStart().Replace("\"", ""), "        ");
            return Convert.ToDecimal(lines[1].ToString().Replace(".", ","));
        }
    }
}