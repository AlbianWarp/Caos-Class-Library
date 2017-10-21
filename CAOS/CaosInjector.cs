using System;
using System.Text;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace CAOS
{
    public static class CaosInjector
    {
        private static Mutex Mutex;
        private static MemoryMappedFile MemFile;
        private static MemoryMappedViewAccessor MemViewAccessor;
        private static EventWaitHandle ResultEventHandle;
        private static EventWaitHandle RequestRventHandle;
        private static string GameName;

        public static void SetGame(string gameName)
        {
            GameName = gameName;
            //It seems to me that these exceptions shouldn't be
            //  thrown from the initializer. But it seems to
            //  be mostly an opionion-based thing w/o any
            //  standard best practices. -JG

            //InitInjector();
            //CloseInjector();
        }

        /// <summary>
        /// This might not be necessary.
        ///     Hmm -JG
        /// </summary>
        public static bool CanConnectToGame()
        {
            try
            {
                InitInjector();
                CloseInjector();
                return true;
            }
            catch (NoGameCaosException)
            {
                return false;
            }
        }

        private static void InitInjector()
        {
            try
            {
                Mutex = Mutex.OpenExisting(GameName + "_mutex");
                MemFile = MemoryMappedFile.OpenExisting(GameName + "_mem");
                MemViewAccessor = MemFile.CreateViewAccessor();
                ResultEventHandle = EventWaitHandle.OpenExisting(GameName + "_result");
                RequestRventHandle = EventWaitHandle.OpenExisting(GameName + "_request");
            }
            catch (Exception e)
                when (e is WaitHandleCannotBeOpenedException
                || e is System.IO.FileNotFoundException)
            {
                throw new NoGameCaosException("No running game engine found.", e);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new NoGameCaosException("Cannot find or access any running game engine.", e);
            }
        }

        private static void CloseInjector()
        {
            RequestRventHandle.Close();
            ResultEventHandle.Close();
            MemViewAccessor.Dispose();
            MemFile.Dispose();
            Mutex.Close();
        }

        public static bool TryAddScriptToScriptorium(int family, int genus, int species, int eventNum, string script)
        {
            CaosResult temp;
            return TryAddScriptToScriptorium(family, genus, species, eventNum, script, out temp);
        }

        public static bool TryAddScriptToScriptorium(int family, int genus, int species, int eventNum, string script, out CaosResult caosResult)
        {
            try
            {
                caosResult = AddScriptToScriptorium(family, genus, species, eventNum, script);
                return true;
            }
            catch (NoGameCaosException)
            {
                caosResult = null;
                return false;
            }
        }

        public static CaosResult AddScriptToScriptorium(int family, int genus, int species, int eventNum, string script)
        {
            return ExecuteCaosGetResult(script, "scrp " + family + " " + genus + " " + species + " " + eventNum);
        }

        public static bool TryExecuteCaosGetResult(string caosAsString)
        {
            CaosResult temp;
            return TryExecuteCaosGetResult(caosAsString, out temp);
        }

        public static bool TryExecuteCaosGetResult(string caosAsString, out CaosResult caosResult)
        {
            try
            {
                caosResult = ExecuteCaosGetResult(caosAsString);
                return true;
            }
            catch (NoGameCaosException)
            {
                caosResult = null;
                return false;
            }
        }

        public static CaosResult ExecuteCaosGetResult(string CaosAsString, string Action = "execute")
        {
            InitInjector();
            byte[] CaosBytes = Encoding.UTF8.GetBytes(Action + "\n" + CaosAsString + "\n");
            int BufferPosition = 24;
            Mutex.WaitOne(1000);
            foreach (byte Byte in CaosBytes)
            {
                MemViewAccessor.Write(BufferPosition, Byte);
                BufferPosition++;
            }
            RequestRventHandle.Set();
            ResultEventHandle.WaitOne(5000);
            int ResultSize = MemViewAccessor.ReadInt16(12);
            byte[] ResultBytes = new byte[ResultSize];
            int ResultCode = Convert.ToInt16(MemViewAccessor.ReadByte(8));
            int ProcessID = Convert.ToInt16(MemViewAccessor.ReadByte(4));
            for (int i = 0; i < ResultSize; i++)
            {
                ResultBytes[i] = MemViewAccessor.ReadByte(24 + i);
            }
            for (int i = 0; i < CaosBytes.Length; i++)
            {
                MemViewAccessor.Write(24 + i, (byte)0);
            }
            for (int i = 0; i < ResultSize; i++)
            {
                MemViewAccessor.Write(24 + i, (byte)0);
            }
            Mutex.ReleaseMutex();
            CloseInjector();
            Thread.Sleep(50);
            return new CaosResult(ResultCode, Encoding.UTF8.GetString(ResultBytes), ProcessID);
        }

        public static int ProcessID()
        {
            Mutex.WaitOne();
            int ProcessID = MemViewAccessor.ReadInt16(4);
            Mutex.ReleaseMutex();
            return ProcessID;
        }
    }
    public class CaosResult
    {
        public int ResultCode { get; private set; }
        public bool Succeded { get; private set; }
        public int ProcessId { get; private set; }
        public string Content { get; private set; }

        public CaosResult(int resultCode, string content, int processID)
        {
            this.ResultCode = resultCode;
            this.Succeded = (resultCode == 0);
            this.Content = content;
            this.ProcessId = processID;
        }
    }
}
