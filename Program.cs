using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Runtime.InteropServices;

class Program
{
    static async Task Main(string[] args)
    {
        // Verifica se o programa está sendo executado como administrador
        if (!IsAdministrator())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("O script precisa ser executado como administrador para funcionar corretamente.");
            Console.ReadLine();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("O script está sendo executado com privilégios administrativos.");

        // Carrega o arquivo de configuração INI
        string iniFilePath = Directory.GetCurrentDirectory() + "\\config.ini";
        if (!File.Exists(iniFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("O arquivo de configuração 'config.ini' não foi encontrado.");
            return;
        }

        IniFile ini = new IniFile(iniFilePath);

        string dbAccessPath = ini.Read("DbAccessPath", "Paths");
        string appServerPath = ini.Read("AppServerPath", "Paths");
        string arguments = ini.Read("Args", "Arguments");

        // Inicia os aplicativos
        // Executar as aplicações em paralelo
        var taskDbAccess = Task.Run(() => StartApp(dbAccessPath, arguments));
        var taskAppServer = Task.Run(() => StartApp(appServerPath, arguments));

        // Aguardar todas as tarefas finalizarem
        await Task.WhenAll(taskDbAccess, taskAppServer);

        Console.WriteLine("Todos os aplicativos foram iniciados.");
    }

    // Função para verificar se está rodando como administrador
    private static bool IsAdministrator()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    // Função para iniciar o aplicativo
    private static void StartApp(string appPath, string arguments)
    {
        if (File.Exists(appPath))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = appPath,
                    Arguments = arguments,
                    UseShellExecute = true
                });
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"O aplicativo {appPath} foi iniciado com sucesso.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Falha ao iniciar o aplicativo {appPath}: {ex.Message}");
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"O caminho do aplicativo não foi encontrado: {appPath}");
        }
        Console.ReadLine();
    }
}

// Classe para manipular o arquivo INI
public class IniFile
{
    private string path;

    [DllImport("kernel32")]
    private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

    [DllImport("kernel32")]
    private static extern int GetPrivateProfileString(string section, string key, string def, System.Text.StringBuilder retVal, int size, string filePath);

    public IniFile(string iniPath)
    {
        path = iniPath;
    }

    public string Read(string key, string section)
    {
        var retVal = new System.Text.StringBuilder(255);
        GetPrivateProfileString(section, key, "", retVal, 255, path);
        return retVal.ToString();
    }

    public void Write(string key, string value, string section)
    {
        WritePrivateProfileString(section, key, value, path);
    }

    public void DeleteKey(string key, string section)
    {
        Write(key, null, section);
    }

    public void DeleteSection(string section)
    {
        Write(null, null, section);
    }

    public bool KeyExists(string key, string section)
    {
        return Read(key, section).Length > 0;
    }
}
