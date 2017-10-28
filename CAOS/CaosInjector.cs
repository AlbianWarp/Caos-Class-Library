using System;
using System.Text;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace CAOS
{
    public class CaosInjector
    {
        private Mutex Mutex;
        private MemoryMappedFile MemFile;
        private MemoryMappedViewAccessor MemViewAccessor;
        private EventWaitHandle ResultEventHandle;
        private EventWaitHandle RequestRventHandle;
        private string GameName;

        public CaosInjector(string gameName)
        {
            GameName = gameName;
        }

        public bool CanConnectToGame()
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

        private void InitInjector()
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

        private void CloseInjector()
        {
            RequestRventHandle.Close();
            ResultEventHandle.Close();
            MemViewAccessor.Dispose();
            MemFile.Dispose();
            Mutex.Close();
        }

        public bool TryAddScriptToScriptorium(int family, int genus, int species, int eventNum, string script)
        {
            CaosResult temp;
            return TryAddScriptToScriptorium(family, genus, species, eventNum, script, out temp);
        }

        public bool TryAddScriptToScriptorium(int family, int genus, int species, int eventNum, string script, out CaosResult caosResult)
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

        public CaosResult AddScriptToScriptorium(int family, int genus, int species, int eventNum, string script)
        {
            return ExecuteCaos(script, "scrp " + family + " " + genus + " " + species + " " + eventNum);
        }

        public bool TryExecuteCaos(string caosAsString)
        {
            CaosResult temp;
            return TryExecuteCaos(caosAsString, out temp);
        }

        public bool TryExecuteCaos(string caosAsString, out CaosResult caosResult)
        {
            try
            {
                caosResult = ExecuteCaos(caosAsString);
                return true;
            }
            catch (NoGameCaosException)
            {
                caosResult = null;
                return false;
            }
        }

        public CaosResult ExecuteCaos(string caosAsString, string action = "execute")
        {
            //Need more exception checking here - JG
            InitInjector();
            byte[] caosBytes = Encoding.UTF8.GetBytes(action + "\n" + caosAsString + "\n");
            int bufferPosition = 24;
            Mutex.WaitOne(1000);
            foreach (byte Byte in caosBytes)
            {
                MemViewAccessor.Write(bufferPosition, Byte);
                bufferPosition++;
            }
            RequestRventHandle.Set();
            ResultEventHandle.WaitOne(5000);
            int resultSize = MemViewAccessor.ReadInt16(12);
            byte[] resultBytes = new byte[resultSize];
            int resultCode = Convert.ToInt16(MemViewAccessor.ReadByte(8));
            int processID = Convert.ToInt16(MemViewAccessor.ReadByte(4));
            for (int i = 0; i < resultSize; i++)
            {
                resultBytes[i] = MemViewAccessor.ReadByte(24 + i);
            }
            for (int i = 0; i < caosBytes.Length; i++)
            {
                MemViewAccessor.Write(24 + i, (byte)0);
            }
            for (int i = 0; i < resultSize; i++)
            {
                MemViewAccessor.Write(24 + i, (byte)0);
            }
            Mutex.ReleaseMutex();
            CloseInjector();
            Thread.Sleep(50);
            return new CaosResult(resultCode, Encoding.UTF8.GetString(resultBytes), processID);
        }

        public int ProcessID()
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
        public bool Success { get; private set; }
        public int ProcessId { get; private set; }
        public string Content { get; private set; }

        public CaosResult(int resultCode, string content, int processID)
        {
            this.ResultCode = resultCode;
            this.Success = (resultCode == 0);
            this.Content = content;
            this.ProcessId = processID;
        }
    }
}
