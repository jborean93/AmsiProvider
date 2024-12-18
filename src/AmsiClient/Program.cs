using AmsiClient.Native;
using System;
using System.ComponentModel;

namespace AmsiClient;

public static class Program
{
    public static int Main(string[] args)
    {
        int res = Amsi.AmsiInitialize("AmsiTest", out var amsiContext);
        if (res != 0)
        {
            throw new Win32Exception(res);
        }

        try
        {
            res = Amsi.AmsiOpenSession(amsiContext, out var amsiSession);
            if (res != 0)
            {
                throw new Win32Exception(res);
            }

            try
            {
                AmsiNotifyOperation(amsiContext, "testing notify", "notify content app");
                AmsiScanBuffer(amsiContext, amsiSession, "testing scan buffer", "scan buffer app");
                return 0;
            }
            finally
            {
                Amsi.AmsiCloseSession(amsiContext, amsiSession);
            }
        }
        finally
        {
            Amsi.AmsiUninitialize(amsiContext);
        }
    }

    public static unsafe void AmsiNotifyOperation(
        nint context,string content,
        string contentName)
    {
        fixed (char* buffer = content)
        {
            int res = Amsi.AmsiNotifyOperation(
                context,
                (nint)buffer,
                content.Length * sizeof(char),
                contentName,
                out var result);
            Console.WriteLine("AmsiNotifyOperation [{0}] - {1} - {2}", content, res, result);
        }
    }

    public static unsafe void AmsiScanBuffer(
        nint context,
        nint session,
        string content,
        string contentName)
    {
        fixed (char* buffer = content)
        {
            int res = Amsi.AmsiScanBuffer(
                context,
                (nint)buffer,
                content.Length * sizeof(char),
                contentName,
                session,
                out var result);
            Console.WriteLine("AmsiScanBuffer [{0}] - {1} - {2}", content, res, result);
        }
    }

    public static void AmsiScanString(
        nint context,
        nint session,
        string content,
        string contentName)
    {
        int res = Amsi.AmsiScanString(
            context,
            content,
            contentName,
            session,
            out var result);
        Console.WriteLine("AmsiScanString [{0}] - {1} - {2}", content, res, result);
    }
}
