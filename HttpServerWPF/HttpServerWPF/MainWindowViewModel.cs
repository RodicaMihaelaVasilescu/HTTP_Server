using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HttpServerWPF
{
  public class MainWindowViewModel : INotifyPropertyChanged
  {
    private Socket httpServer;
    private int serverPort = 80;
    private Thread thread;
    private string serverPortText = "11111";
    private string serverLogsText;
    private bool _isStartButtonEnabled = true;
    private bool _isStopButtonEnabled = false;

    public event PropertyChangedEventHandler PropertyChanged;

    public ICommand StartServerCommand { get; set; }
    public ICommand StopServerCommand { get; set; }

    public string ServerPortText
    {
      get { return serverPortText; }
      set
      {
        serverPortText = value;
        NotifyPropertyChanged();
      }
    }

    public string ServerLogsText
    {
      get { return serverLogsText; }
      set
      {
        serverLogsText = value;
        NotifyPropertyChanged();
      }
    }

    public bool IsStartButtonEnabled
    {
      get { return _isStartButtonEnabled; }
      set
      {
        _isStartButtonEnabled = value;
        NotifyPropertyChanged();
      }
    }

    public bool IsStopButtonEnabled
    {
      get { return _isStopButtonEnabled; }
      set
      {
        _isStopButtonEnabled = value;
        NotifyPropertyChanged();
      }
    }


    public MainWindowViewModel()
    {
      StartServerCommand = new DelegateCommand(StartServerCommandExecute);
      StopServerCommand = new DelegateCommand(StopServerCommandExecute);
    }

    private void StartServerCommandExecute()
    {
      try
      {
        httpServer = new Socket(SocketType.Stream, ProtocolType.Tcp);

        try
        {
          serverPort = int.Parse(serverPortText.ToString());

          if (serverPort > 65535 || serverPort <= 0)
          {
            throw new Exception("Server Port not within the range");
          }
        }
        catch (Exception ex)
        {
          serverPort = 80;
          ServerLogsText = "Server Failed to Start on Specified Port \n";
        }

        thread = new Thread(new ThreadStart(connectionThreadMethod));
        thread.Start();

        // Disable and Enable Buttons
        IsStartButtonEnabled = false;
        IsStopButtonEnabled = true;

      }
      catch (Exception ex)
      {
        Console.WriteLine("Error while starting the server");
        ServerLogsText = "Server Starting Failed \n";
      }
      ServerLogsText = "Server started, Port: " + ServerPortText;
    }

    private void StopServerCommandExecute()
    {
      try
      {
        // Close the Socket
        httpServer.Close();

        // Kill the Thread
        thread.Abort();

        // Disable and Enable Buttons
        IsStartButtonEnabled = true;
        IsStopButtonEnabled = false;
      }
      catch (Exception ex)
      {
        Console.WriteLine("Stopping Failed");
      }

      ServerLogsText = null;
    }

    private void connectionThreadMethod()
    {
      try
      {
        IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, serverPort);
        httpServer.Bind(endpoint);
        httpServer.Listen(1);
        startListeningForConnection();
      }
      catch (Exception ex)
      {
        Console.WriteLine("I could nt start");
      }
    }

    private void startListeningForConnection()
    {
      while (true)
      {
        DateTime time = DateTime.Now;

        String data = "";
        byte[] bytes = new byte[2048];

        Socket client = httpServer.Accept(); // Blocking Statement

        // Reading the inbound connection data
        while (true)
        {
          int numBytes = client.Receive(bytes);
          data += Encoding.ASCII.GetString(bytes, 0, numBytes);

          if (data.IndexOf("\r\n") > -1)
            break;
        }

        // Data Read

        ServerLogsText += "\r\n\r\n";
        ServerLogsText += data;
        ServerLogsText += "\n\n------ End of Request -------";

        // Send back the Response
        String resHeader = "HTTP/1.1 200 Everything is Fine\nServer: my_csharp_server\nContent-Type: text/html; charset: UTF-8\n\n";
        string path = Path.GetFullPath("HTML_Document.txt");
        string resBody = File.ReadAllText(path);

        String resStr = resHeader + resBody;

        byte[] resData = Encoding.ASCII.GetBytes(resStr);

        client.SendTo(resData, client.RemoteEndPoint);

        client.Close();
      }
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }
  }
}
