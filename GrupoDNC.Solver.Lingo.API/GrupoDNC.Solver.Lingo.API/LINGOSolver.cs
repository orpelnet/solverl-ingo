using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GrupoDNC.Solver.Lingo.API
{
    [StructLayout(LayoutKind.Sequential)]
    public class CallbackData
    {
        public int nIterations;

        // Constructor:    
        public CallbackData()
        {
            nIterations = 0;
        }
    }

    public class LingoSolver
    {
        [STAThread]
        public ResultLingo Solver(FileInfo fileModelo, string fileLog)
        {
            try
            {
                ResultLingo result = new ResultLingo();
                IntPtr pLingoEnv;
                int nError = -1, nPointersNow = -1;
                double dObjective = -1, dStatus = -1, dCusto = -1, dProducao = -1;
                
                result.DStatus = -1;
                result.DObjective = -1;
                result.DCusto = -1;
                result.DProducao = -1;

                unsafe
                {
                    fixed (
                    double* dValuesPrimal = new double[1])
                    {
                        //Monta o Ambiente
                        pLingoEnv = lingo.LScreateEnvLng();
                        if (pLingoEnv == IntPtr.Zero)
                        {
                            Console.WriteLine("Unable to create Lingo environment.\n");
                            goto FinalExit;
                        }

                        //Abre o arquivo lingo
                        nError = lingo.LSopenLogFileLng(pLingoEnv, fileModelo.DirectoryName + "\\" + fileLog);
                        if (nError != lingo.LSERR_NO_ERROR_LNG) goto ErrorExit;

                        //Inicia classe e metodos de Callback
                        CallbackData cbd = new CallbackData();
                        lingo.typCallback cb = new lingo.typCallback(LngCallback.MyCallback);
                        
                        //Chama o callback
                        //nError = lingo.LSsetCallbackSolverLng(pLingoEnv, cb, cbd);
                        //if (nError != lingo.LSERR_NO_ERROR_LNG) goto ErrorExit;

                        // Pointeiro do Status
                        nError = lingo.LSsetPointerLng(pLingoEnv, &dStatus, ref nPointersNow);
                        if (nError != lingo.LSERR_NO_ERROR_LNG) goto ErrorExit;

                        // Pointeiro da Função Objetivo
                        nError = lingo.LSsetPointerLng(pLingoEnv, &dObjective, ref nPointersNow);
                        if (nError != lingo.LSERR_NO_ERROR_LNG) goto ErrorExit;

                        // Pointeiro da varivel Producao
                        nError = lingo.LSsetPointerLng(pLingoEnv, &dProducao, ref nPointersNow);
                        if (nError != lingo.LSERR_NO_ERROR_LNG) goto ErrorExit;
                        
                        // Pointeiro do array Primal
                        nError = lingo.LSsetPointerLng(pLingoEnv, dValuesPrimal, ref nPointersNow);
                        if (nError != lingo.LSERR_NO_ERROR_LNG) goto ErrorExit;
                                                                        
                        // Pointeiro da variavel Custo
                        nError = lingo.LSsetPointerLng(pLingoEnv, &dCusto, ref nPointersNow);
                        if (nError != lingo.LSERR_NO_ERROR_LNG) goto ErrorExit;
                        
                        //Script
                        string cScript =
                        "set echoin 1 \n take " + fileModelo + " \n go \n quit \n";

                        // Executa script
                        nError = lingo.LSexecuteScriptLng(pLingoEnv, cScript);
                        if (nError != lingo.LSERR_NO_ERROR_LNG) goto ErrorExit;
                        
                        result.DObjective = dObjective;
                        result.DStatus = dStatus;
                        result.DCusto = dCusto;
                        result.DInteracoes = cbd.nIterations;
                        result.DProducao = dProducao;

                        // Close the log file
                        lingo.LScloseLogFileLng(pLingoEnv);
                    }
                }

                goto NormalExit;

                ErrorExit:
                Console.WriteLine("LINGO Error Code: {0}\n", nError);

                NormalExit:
                
                // Free Lingo's envvironment to avoid a memory leak
                lingo.LSdeleteEnvLng(pLingoEnv);

                FinalExit:
                Console.WriteLine("");
                return result;
            }
            catch
            {
                throw;
            }
        }
    }

    public class LngCallback
    {
        public LngCallback()
        {
        }

        public static int MyCallback(IntPtr pLingoEnv, int nReserved, IntPtr pMyData)
        {
            CallbackData cb = new CallbackData();
            Marshal.PtrToStructure(pMyData, cb);

            int nIterations = -1, nErr;
            nErr = lingo.LSgetCallbackInfoLng(pLingoEnv,
            lingo.LS_IINFO_ITERATIONS_LNG, ref nIterations);
            if (nErr == lingo.LSERR_NO_ERROR_LNG && nIterations != cb.nIterations)
            {
                cb.nIterations = nIterations;

            }

            Marshal.StructureToPtr(cb, pMyData, true);
            return 0;
        }
    }

    public class ResultLingo
    {
        public double DStatus { get; set; }
        public double DObjective { get; set; }
        public double DCusto { get; set; }
        public double DInteracoes { get; set; }
        public double DProducao { get; set; }
    }
}