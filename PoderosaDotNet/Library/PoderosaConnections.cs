using System;
using System.Reflection;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;

using Poderosa;
using Poderosa.Boot;
using Poderosa.Forms;
using Poderosa.Sessions;
using Poderosa.View;
using Poderosa.Commands;
using Poderosa.Terminal;
using Poderosa.Protocols;
using Poderosa.Preferences;
using Poderosa.SerialPort;

using Granados;
using Granados.SSH2;

namespace PoderosaDotNet
{
    public enum ProtocolType
    {
        Telnet,
        SSH1,
        SSH2,
        Cywin,
        XModem,
        ZModem,
        Raw,
        RLogin,
        Serial,
        //Socks,
        SCP,
        SFTP
    }
        
    internal class InternalPoderosaInstance
    {
        InternalPoderosaWorld _internalPoderosaWorld;
        string _homeDirectory;

        Dictionary<int,InternalTerminalInstance> _terminals;

        public InternalPoderosaInstance()
        {
            _terminals=new Dictionary<int,InternalTerminalInstance>();

            string[] args = new string[0];
            _homeDirectory = Directory.GetCurrentDirectory();

            _internalPoderosaWorld = new InternalPoderosaWorld(new PoderosaStartupContext(PluginManifest.CreateByText(BuildPluginList()), _homeDirectory, _homeDirectory, args, null));
            if (_internalPoderosaWorld != null)
            {
                ((InternalPoderosaWorld)_internalPoderosaWorld).Start();
            }
        }
        private string BuildPluginList()
        {
            StringBuilder bld = new StringBuilder();

            bld.Append("manifest {\r\n  " + Assembly.GetExecutingAssembly().ManifestModule.Name + " {\r\n");
            bld.Append("  plugin=Poderosa.Preferences.PreferencePlugin\r\n");
            bld.Append("  plugin=Poderosa.Serializing.SerializeServicePlugin\r\n");
            bld.Append("  plugin=Poderosa.Commands.CommandManagerPlugin\r\n");
            bld.Append("  plugin=Poderosa.Forms.WindowManagerPlugin\r\n");
            bld.Append("  plugin=Poderosa.Protocols.ProtocolsPlugin\r\n");
            bld.Append("  plugin=Poderosa.Terminal.TerminalEmulatorPlugin\r\n");
            bld.Append("  plugin=Poderosa.Sessions.SessionManagerPlugin\r\n");
            bld.Append("  plugin=Poderosa.Sessions.TerminalSessionsPlugin\r\n");
            bld.Append("  plugin=Poderosa.Sessions.CygwinPlugin\r\n");
            bld.Append("  plugin=Poderosa.Sessions.TelnetSSHPlugin\r\n");
            bld.Append("  plugin=Poderosa.Sessions.ShortcutFilePlugin\r\n");
            bld.Append("  plugin=Poderosa.Usability.UsabilityPlugin\r\n");
            bld.Append("  plugin=Poderosa.Usability.MRUPlugin\r\n");
            bld.Append("  plugin=Poderosa.Usability.TerminalUIPlugin\r\n");
            bld.Append("  plugin=Poderosa.Usability.SSHUtilPlugin\r\n");
            bld.Append("  plugin=Poderosa.Usability.OptionDialogPlugin\r\n");
            bld.Append("  plugin=Poderosa.Usability.StartupActionPlugin\r\n");
            bld.Append("  plugin=Poderosa.SerialPort.SerialPortPlugin\r\n");
            bld.Append("  plugin=Poderosa.XZModem.XZModemPlugin\r\n");
            bld.Append("  plugin=Poderosa.PortForwardingCommand.PortForwardingCommandPlugin\r\n");
            bld.Append("  plugin=Poderosa.MacroInternal.MacroPlugin\r\n");
            bld.Append("  plugin=Poderosa.LogViewer.PoderosaLogViewerPlugin\r\n");
            bld.Append("}\r\n}");
            return bld.ToString();
        }

        public WindowManagerPlugin WindowManagerPlugin
        {
            get
            {
                return (WindowManagerPlugin)_internalPoderosaWorld.PluginManager.FindPlugin(WindowManagerPlugin.PLUGIN_ID, typeof(WindowManagerPlugin));
            }
        }
        public TerminalSessionsPlugin TerminalSessionsPlugin
        {
            get
            {
                return (TerminalSessionsPlugin)_internalPoderosaWorld.PluginManager.FindPlugin(TerminalSessionsPlugin.PLUGIN_ID, typeof(TerminalSessionsPlugin));
            }
        }
        public SessionManagerPlugin SessionManagerPlugin
        {
            get
            {
                return (SessionManagerPlugin)_internalPoderosaWorld.PluginManager.FindPlugin(SessionManagerPlugin.PLUGIN_ID, typeof(SessionManagerPlugin));
            }
        }
        public ProtocolsPlugin ProtocolsPlugin
        {
            get
            {
                return (ProtocolsPlugin)_internalPoderosaWorld.PluginManager.FindPlugin(ProtocolsPlugin.PLUGIN_ID, typeof(ProtocolsPlugin));
            }
        }
        public SerialPortPlugin SerialPortPlugin
        {
            get
            {
                return (SerialPortPlugin)_internalPoderosaWorld.PluginManager.FindPlugin(SerialPortPlugin.PLUGIN_ID, typeof(SerialPortPlugin));
            }
        }
        public IProtocolService ProtocolService
        {
            get
            {
                return this.TerminalSessionsPlugin.ProtocolService;
            }
        }
        
        public MainWindow NewMain
        {
            get
            {
                return this.WindowManagerPlugin.CreateLibraryMainWindow();
            }
        }

        public InternalTerminalInstance NewTerminal(ProtocolType _protocolType,ITerminalParameter _terminalParamaters)
        {
            InternalTerminalInstance _instance;
            _instance = new InternalTerminalInstance(_protocolType, this, _terminalParamaters);
            return _instance;
        }

        public InternalTerminalInstance GetInstance(int _hashCode)
        {
               InternalTerminalInstance _instance;
               _terminals.TryGetValue(_hashCode, out _instance);
                return _instance;
        }

    }

    internal class InternalTerminalInstance
    {
        private InternalPoderosaInstance _basePoderosaInstance;
        private MainWindow _window;
        private IPoderosaView _terminalView;

        private TerminalSettings _terminalSettings;
        private TerminalSession _terminalSession;
        private ITerminalParameter _terminalParameter;

        private ITerminalConnection _terminalConnection;
        private ISynchronizedConnector _synchronizedConnector;
        private IInterruptable _asyncConnection;

        private ProtocolType _protocol;

        public InternalTerminalInstance(ProtocolType protocol, InternalPoderosaInstance _internalPoderosaWorld, ITerminalParameter _IterminalParameter)
        {
            _basePoderosaInstance=_internalPoderosaWorld;
            _window = _basePoderosaInstance.WindowManagerPlugin.CreateLibraryMainWindow();

            _protocol = protocol;

            _terminalSettings = new TerminalSettings();
            _terminalParameter = _IterminalParameter;
            _terminalView = _window.ViewManager.GetCandidateViewForNewDocument();
        }
        
        public void Connect()
        {
            try
            {
                switch (_protocol)
                {
                    case ProtocolType.Cywin:
                        _synchronizedConnector = _basePoderosaInstance.ProtocolService.CreateFormBasedSynchronozedConnector(_window);
                        _asyncConnection = _basePoderosaInstance.ProtocolService.AsyncCygwinConnect(_synchronizedConnector.InterruptableConnectorClient, (ICygwinParameter)_terminalParameter);
                        _terminalConnection = _synchronizedConnector.WaitConnection(_asyncConnection, _basePoderosaInstance.TerminalSessionsPlugin.TerminalSessionOptions.TerminalEstablishTimeout);
                        break;
                    case ProtocolType.Raw:
                        break;
                    case ProtocolType.RLogin:
                        break;
                    case ProtocolType.Serial:
                        _terminalConnection =(ITerminalConnection) SerialPortUtil.CreateNewSerialConnection(_window,(SerialTerminalParam)_terminalParameter,(SerialTerminalSettings)_terminalSettings);
                        break;
                    case ProtocolType.SSH1:
                    case ProtocolType.SSH2:
                        _synchronizedConnector = _basePoderosaInstance.ProtocolService.CreateFormBasedSynchronozedConnector(_window);
                        _asyncConnection = _basePoderosaInstance.ProtocolService.AsyncSSHConnect(_synchronizedConnector.InterruptableConnectorClient, (ISSHLoginParameter)_terminalParameter);
                        _terminalConnection = _synchronizedConnector.WaitConnection(_asyncConnection, _basePoderosaInstance.TerminalSessionsPlugin.TerminalSessionOptions.TerminalEstablishTimeout);
                        break;
                    case ProtocolType.Telnet:
                        _synchronizedConnector = _basePoderosaInstance.ProtocolService.CreateFormBasedSynchronozedConnector(_window);
                        _asyncConnection = _basePoderosaInstance.ProtocolService.AsyncTelnetConnect(_synchronizedConnector.InterruptableConnectorClient, (ITCPParameter)_terminalParameter);
                        _terminalConnection = _synchronizedConnector.WaitConnection(_asyncConnection, _basePoderosaInstance.TerminalSessionsPlugin.TerminalSessionOptions.TerminalEstablishTimeout);
                        break;
                    default:
                        _terminalConnection = null;
                        break;
                }
                _terminalSession = new TerminalSession(_terminalConnection, _terminalSettings);
                _basePoderosaInstance.SessionManagerPlugin.StartNewSession(_terminalSession, _terminalView);
                _basePoderosaInstance.SessionManagerPlugin.ActivateDocument(_terminalSession.Terminal.IDocument, ActivateReason.InternalAction);
            }
            catch (Exception ex)
            {
                RuntimeUtil.ReportException(ex);
                //return CommandResult.Failed;
            }
        }
        public Form asForm()
        {
            _window.ViewManager.RootControl.Visible = true;
            return _window.AsForm();
        }
    }

    public class SSH2Session 
    {
        InternalPoderosaInstance _instance;
        InternalTerminalInstance _terminal;
        SSHLoginParameter _SSHParms;

        public SSH2Session()
        {
            _SSHParms = new SSHLoginParameter();
            _SSHParms.Account = "root";
            _SSHParms.PasswordOrPassphrase = "Pasw0rd";
            _SSHParms.Destination = "192.168.2.1";
            _SSHParms.Port = 22;
            _SSHParms.AuthenticationType = Granados.AuthenticationType.Password;

            _instance =new  InternalPoderosaInstance();
            _terminal =_instance.NewTerminal(ProtocolType.SSH2, _SSHParms);
        }

        public void Connect()
        {
            _terminal.Connect();
        }

        public Form asForm
        {
            get
            {
                return _terminal.asForm();
            }
        }

        
    }

    //send protocols- scp,xzmodem,sftp
    //firewall stuff-socks,portforwarding
}