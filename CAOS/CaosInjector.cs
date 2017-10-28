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

/*
Offset  Size    C Type          Memory Buffer Layout
0       4       CHAR            4 characters which should be 'c2e@'. If it is not this then either the buffer is corrupt or you're looking in the wrong place.
4       4       DWORD           Process id of the game engine. By retrieving this you can use Windows API functions to ensure that the game engine is still running, or be notified if it is closed, when waiting for results.
8       4       int             Result code from the last game engine request submitted. A '0' is success, a '1' is failed. If there is a failure the reason for the failure is in the data section below.
12      4       unsigned int    The size in bytes of the data returned in the data buffer.
16      4       unsigned int    The size of the shared memory buffer. No requests or replies can be larger than this. It is currently set to about 1MB in the game engine.
20      4       int             Padding to align data on an 8 byte boundary.
24 -variable-Request dependant- This is where you store your request and retrieve your replies. Depending on the type of request it can be a null terminated array of characters or binary data.
copied from double.nz/creatures/developer/sharedmemory.htm
*/

        private const int POS_ENGINE_NAME = 0;
        private const int POS_PROCESS_ID = 4;
        private const int POS_RESULT_CODE = 8;
        private const int POS_RESULT_SIZE = 12;
        private const int POS_MEMORY_BUFFER_SIZE = 16; //documentation available is unclear if this includes the variables
        private const int POS_BUFFER = 24;

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
            catch (NoRunningEngineException)
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
                throw new NoRunningEngineException("No running game engine found.", e);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new NoRunningEngineException("Cannot find or access any running game engine.", e);
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
            catch (NoRunningEngineException)
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
            catch (NoRunningEngineException)
            {
                caosResult = null;
                return false;
            }
        }

        public CaosResult ExecuteCaos(string caosAsString, string action = "execute")
        {
            //Need more exception checking here - JG
            InitInjector();
            byte[] caosBytes = Encoding.UTF8.GetBytes($"{action}\n{caosAsString}\0");
            int bufferPosition = POS_BUFFER;
            Mutex.WaitOne(1000);

            foreach (byte b in caosBytes)
            {
                MemViewAccessor.Write(bufferPosition, b);
                bufferPosition++;
            }

            RequestRventHandle.Set();
            ResultEventHandle.WaitOne(5000);
            int resultSize = MemViewAccessor.ReadInt16(POS_RESULT_SIZE);
            byte[] resultBytes = new byte[resultSize];
            int resultCode = Convert.ToInt16(MemViewAccessor.ReadByte(8));
            int processID = Convert.ToInt16(MemViewAccessor.ReadByte(4));

            for (int i = POS_BUFFER; i < resultSize; i++)
            {
                resultBytes[i] = MemViewAccessor.ReadByte(i);
            }

            int overwriteLength = (caosBytes.Length > resultSize) ? caosBytes.Length : resultSize;
            for (int i = POS_BUFFER; i < overwriteLength; i++)
            {
                MemViewAccessor.Write(POS_BUFFER + i, (byte)0);
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
