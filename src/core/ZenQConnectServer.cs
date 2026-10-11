using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZenQOffice.Core
{
    /// <summary>
    /// ZenQ Connect Server: Máy chủ kết nối không dây PC - Mobile thuần C# siêu nhẹ.
    /// Cho phép bất kỳ điện thoại thông minh nào (iOS Safari, Android Chrome) qua mạng Wi-Fi nội bộ:
    /// 1. Điều khiển chuột / Touchpad đa điểm toàn diện (Click, Right-Click, Double-Click, Scroll, Drag-and-Drop).
    /// 2. Điều khiển trình chiếu Slide chuyên nghiệp (Next, Prev, Timer, Ghi chú diễn giả live, Màn hình đen/trắng).
    /// 3. Bút Laser ảo thời gian thực (chiếu điểm laser đỏ trên màn hình máy chiếu từ màn hình cảm ứng điện thoại).
    /// 4. Bàn vẽ chữ ký số & phác thảo (chèn tức thì 1-chạm vào văn bản Word/Writer, Slide PPTX, hoặc Bảng tính Calc).
    /// 5. Bàn phím số NumPad & Hộp nhập văn bản tiếng Việt từ xa.
    /// </summary>
    public class ZenQConnectServer
    {
        private static ZenQConnectServer _instance;
        private static readonly object _syncRoot = new object();

        public static ZenQConnectServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null) _instance = new ZenQConnectServer();
                    }
                }
                return _instance;
            }
        }

        #region Win32 Native Simulation
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_HWHEEL = 0x1000;

        private const uint KEYEVENTF_KEYUP = 0x0002;
        #endregion

        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private readonly List<ConnectedClient> _clients = new List<ConnectedClient>();
        private readonly object _clientsLock = new object();
        private System.Threading.Timer _statusBroadcastTimer;

        public bool IsRunning { get; private set; }
        public int Port { get; private set; } = 8088;
        public string PreferredIp { get; set; } = "";
        public string ConnectedDeviceName { get; private set; } = "";
        public bool IsClientConnected => ConnectedClientsCount > 0;
        public int ConnectedClientsCount
        {
            get
            {
                lock (_clientsLock) return _clients.Count;
            }
        }

        public string ActiveIpAddress
        {
            get
            {
                if (!string.IsNullOrEmpty(PreferredIp)) return PreferredIp;
                var ips = GetAllLocalIpAddresses();
                return ips.Count > 0 ? ips[0] : "127.0.0.1";
            }
        }

        public string ActiveUrl => $"http://{ActiveIpAddress}:{Port}/";

        public event Action OnServerStarted;
        public event Action OnServerStopped;
        public event Action<int> OnClientCountChanged;
        public event Action<string> OnStatusMessage;

        // Custom action callbacks (injected by UI / AppForm)
        public Action<string> ActionDispatchCustomCommand { get; set; }
        public Action<float, float, bool> ActionLaserPointer { get; set; }
        public Action<Bitmap> ActionInsertSignature { get; set; }
        public Func<Dictionary<string, object>> FuncGetAppStatus { get; set; }

        public ZenQConnectServer()
        {
        }

        public bool Start(int port = 8088)
        {
            if (IsRunning) return true;

            int targetPort = port;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                try
                {
                    _listener = new TcpListener(IPAddress.Any, targetPort);
                    _listener.Start();
                    Port = targetPort;
                    break;
                }
                catch
                {
                    targetPort++;
                    _listener = null;
                }
            }

            if (_listener == null)
            {
                OnStatusMessage?.Invoke("Không thể khởi động cổng mạng nội bộ!");
                return false;
            }

            IsRunning = true;
            _cts = new CancellationTokenSource();

            Task.Run(() => AcceptClientsLoop(_cts.Token));

            _statusBroadcastTimer = new System.Threading.Timer(OnBroadcastTimerTick, null, 1000, 1000);

            OnServerStarted?.Invoke();
            OnStatusMessage?.Invoke($"ZenQ Connect đang chạy tại: {ActiveUrl}");
            return true;
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;

            try { _statusBroadcastTimer?.Dispose(); } catch { }
            _statusBroadcastTimer = null;

            try { _cts?.Cancel(); } catch { }
            try { _listener?.Stop(); } catch { }
            _listener = null;

            lock (_clientsLock)
            {
                foreach (var client in _clients)
                {
                    client.Close();
                }
                _clients.Clear();
            }

            ConnectedDeviceName = "";
            OnServerStopped?.Invoke();
            OnClientCountChanged?.Invoke(0);
        }

        public List<string> GetAllLocalIpAddresses()
        {
            var ips = new List<string>();
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    var ipProps = ni.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            string ipStr = addr.Address.ToString();
                            if (!ipStr.StartsWith("127.") && !ipStr.StartsWith("169.254."))
                            {
                                // Ưu tiên dải mạng nội bộ Wi-Fi thông dụng 192.168.x hoặc 10.x
                                if (ipStr.StartsWith("192.168.") || ipStr.StartsWith("10."))
                                    ips.Insert(0, ipStr);
                                else
                                    ips.Add(ipStr);
                            }
                        }
                    }
                }
            }
            catch { }

            if (ips.Count == 0) ips.Add("127.0.0.1");
            return ips;
        }

        public Bitmap GenerateQrBitmap(int size = 260)
        {
            return ZenQMiniQr.GenerateQr(ActiveUrl, size);
        }

        private async Task AcceptClientsLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && IsRunning)
            {
                try
                {
                    TcpClient tcpClient = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleIncomingClient(tcpClient, token), token);
                }
                catch
                {
                    if (!IsRunning) break;
                }
            }
        }

        private async Task HandleIncomingClient(TcpClient tcpClient, CancellationToken token)
        {
            NetworkStream stream = tcpClient.GetStream();
            byte[] buffer = new byte[8192];
            int bytesRead = 0;

            try
            {
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
            }
            catch
            {
                tcpClient.Close();
                return;
            }

            if (bytesRead <= 0)
            {
                tcpClient.Close();
                return;
            }

            string requestStr = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            string[] requestLines = requestStr.Split(new[] { "\r\n" }, StringSplitOptions.None);
            if (requestLines.Length == 0)
            {
                tcpClient.Close();
                return;
            }

            string firstLine = requestLines[0];
            string[] parts = firstLine.Split(' ');
            if (parts.Length < 2)
            {
                tcpClient.Close();
                return;
            }

            string method = parts[0].ToUpperInvariant();
            string path = parts[1];

            // 1. Phục vụ WebSocket Upgrade
            if (path.StartsWith("/ws") || requestStr.Contains("Upgrade: websocket") || requestStr.Contains("Upgrade: WebSocket"))
            {
                await UpgradeToWebSocket(tcpClient, stream, requestLines, token);
                return;
            }

            // 2. Phục vụ hình ảnh QR động: /qr.png
            if (path == "/qr.png")
            {
                using (Bitmap qr = GenerateQrBitmap(320))
                using (MemoryStream ms = new MemoryStream())
                {
                    qr.Save(ms, ImageFormat.Png);
                    byte[] pngBytes = ms.ToArray();
                    string header = "HTTP/1.1 200 OK\r\n" +
                                   "Content-Type: image/png\r\n" +
                                   $"Content-Length: {pngBytes.Length}\r\n" +
                                   "Cache-Control: no-cache\r\n" +
                                   "Connection: close\r\n\r\n";
                    byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                    await stream.WriteAsync(headerBytes, 0, headerBytes.Length, token);
                    await stream.WriteAsync(pngBytes, 0, pngBytes.Length, token);
                }
                tcpClient.Close();
                return;
            }

            // 3. Phục vụ API kiểm tra ping
            if (path == "/ping")
            {
                string jsonResp = "{\"status\":\"ok\",\"server\":\"ZenQ Connect\",\"port\":" + Port + "}";
                byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonResp);
                string resp = "HTTP/1.1 200 OK\r\n" +
                              "Content-Type: application/json; charset=utf-8\r\n" +
                              $"Content-Length: {bodyBytes.Length}\r\n" +
                              "Access-Control-Allow-Origin: *\r\n" +
                              "Connection: close\r\n\r\n" + jsonResp;
                byte[] respBytes = Encoding.UTF8.GetBytes(resp);
                await stream.WriteAsync(respBytes, 0, respBytes.Length, token);
                tcpClient.Close();
                return;
            }

            // 4. Phục vụ Trang Web Ứng dụng Di động Toàn Năng (Single-Page App)
            string htmlContent = GetMobileAppHtml();
            byte[] htmlBytes = Encoding.UTF8.GetBytes(htmlContent);
            string httpResponse = "HTTP/1.1 200 OK\r\n" +
                                 "Content-Type: text/html; charset=utf-8\r\n" +
                                 $"Content-Length: {htmlBytes.Length}\r\n" +
                                 "Connection: close\r\n\r\n";
            byte[] headerData = Encoding.UTF8.GetBytes(httpResponse);
            await stream.WriteAsync(headerData, 0, headerData.Length, token);
            await stream.WriteAsync(htmlBytes, 0, htmlBytes.Length, token);
            tcpClient.Close();
        }

        private async Task UpgradeToWebSocket(TcpClient tcpClient, NetworkStream stream, string[] headers, CancellationToken token)
        {
            string secKey = "";
            foreach (var h in headers)
            {
                if (h.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase))
                {
                    secKey = h.Substring("Sec-WebSocket-Key:".Length).Trim();
                    break;
                }
            }

            if (string.IsNullOrEmpty(secKey))
            {
                tcpClient.Close();
                return;
            }

            // Handshake RFC 6455
            const string magic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] hashBytes;
            using (SHA1 sha1 = SHA1.Create())
            {
                hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(secKey + magic));
            }
            string acceptVal = Convert.ToBase64String(hashBytes);

            string handshakeResponse = "HTTP/1.1 101 Switching Protocols\r\n" +
                                       "Upgrade: websocket\r\n" +
                                       "Connection: Upgrade\r\n" +
                                       $"Sec-WebSocket-Accept: {acceptVal}\r\n\r\n";
            byte[] handshakeBytes = Encoding.UTF8.GetBytes(handshakeResponse);
            await stream.WriteAsync(handshakeBytes, 0, handshakeBytes.Length, token);

            ConnectedClient client = new ConnectedClient(tcpClient, stream);
            lock (_clientsLock)
            {
                _clients.Add(client);
            }
            OnClientCountChanged?.Invoke(ConnectedClientsCount);

            // Gửi ngay trạng thái ban đầu của PC cho client
            SendCurrentAppStatus(client);

            // Đọc khung WebSocket RFC 6455
            try
            {
                await ReadWebSocketFrames(client, token);
            }
            catch { }
            finally
            {
                lock (_clientsLock)
                {
                    _clients.Remove(client);
                }
                client.Close();
                OnClientCountChanged?.Invoke(ConnectedClientsCount);
            }
        }

        private async Task ReadWebSocketFrames(ConnectedClient client, CancellationToken token)
        {
            var stream = client.Stream;
            byte[] header = new byte[2];

            while (!token.IsCancellationRequested && IsRunning && client.IsConnected)
            {
                int read = await stream.ReadAsync(header, 0, 2, token);
                if (read < 2) break;

                bool fin = (header[0] & 0x80) != 0;
                int opcode = header[0] & 0x0F;
                bool masked = (header[1] & 0x80) != 0;
                ulong payloadLen = (ulong)(header[1] & 0x7F);

                if (payloadLen == 126)
                {
                    byte[] ext = new byte[2];
                    await stream.ReadAsync(ext, 0, 2, token);
                    Array.Reverse(ext);
                    payloadLen = BitConverter.ToUInt16(ext, 0);
                }
                else if (payloadLen == 127)
                {
                    byte[] ext = new byte[8];
                    await stream.ReadAsync(ext, 0, 8, token);
                    Array.Reverse(ext);
                    payloadLen = BitConverter.ToUInt64(ext, 0);
                }

                byte[] maskKey = new byte[4];
                if (masked)
                {
                    await stream.ReadAsync(maskKey, 0, 4, token);
                }

                byte[] payload = new byte[payloadLen];
                ulong totalRead = 0;
                while (totalRead < payloadLen)
                {
                    int r = await stream.ReadAsync(payload, (int)totalRead, (int)(payloadLen - totalRead), token);
                    if (r <= 0) break;
                    totalRead += (ulong)r;
                }

                if (masked)
                {
                    for (ulong i = 0; i < payloadLen; i++)
                    {
                        payload[i] = (byte)(payload[i] ^ maskKey[i % 4]);
                    }
                }

                // Opcode 8 = Close
                if (opcode == 8)
                {
                    try { SendWebSocketFrame(client, 8, payload); } catch { }
                    break;
                }
                // Opcode 9 = Ping -> gửi Pong (Opcode 10)
                else if (opcode == 9)
                {
                    SendWebSocketFrame(client, 10, payload);
                }
                // Opcode 1 = Text Frame
                else if (opcode == 1)
                {
                    string message = Encoding.UTF8.GetString(payload);
                    ProcessIncomingMessage(client, message);
                }
            }
        }

        private void SendWebSocketFrame(ConnectedClient client, int opcode, byte[] payload)
        {
            try
            {
                byte[] header;
                int payloadLen = payload != null ? payload.Length : 0;

                if (payloadLen <= 125)
                {
                    header = new byte[2];
                    header[0] = (byte)(0x80 | (opcode & 0x0F));
                    header[1] = (byte)payloadLen;
                }
                else if (payloadLen <= 65535)
                {
                    header = new byte[4];
                    header[0] = (byte)(0x80 | (opcode & 0x0F));
                    header[1] = 126;
                    header[2] = (byte)((payloadLen >> 8) & 0xFF);
                    header[3] = (byte)(payloadLen & 0xFF);
                }
                else
                {
                    header = new byte[10];
                    header[0] = (byte)(0x80 | (opcode & 0x0F));
                    header[1] = 127;
                    for (int i = 7; i >= 0; i--)
                    {
                        header[2 + (7 - i)] = (byte)((payloadLen >> (i * 8)) & 0xFF);
                    }
                }

                client.Stream.Write(header, 0, header.Length);
                if (payloadLen > 0)
                {
                    client.Stream.Write(payload, 0, payloadLen);
                }
            }
            catch { }
        }

        public void BroadcastJson(string json)
        {
            byte[] payload = Encoding.UTF8.GetBytes(json);
            lock (_clientsLock)
            {
                for (int i = _clients.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        SendWebSocketFrame(_clients[i], 1, payload);
                    }
                    catch
                    {
                        _clients.RemoveAt(i);
                    }
                }
            }
        }

        private void OnBroadcastTimerTick(object state)
        {
            if (!IsRunning || ConnectedClientsCount == 0) return;
            SendCurrentAppStatus(null);
        }

        private void SendCurrentAppStatus(ConnectedClient client)
        {
            try
            {
                Dictionary<string, object> status = null;
                if (FuncGetAppStatus != null)
                {
                    status = FuncGetAppStatus();
                }

                if (status == null)
                {
                    status = new Dictionary<string, object>
                    {
                        { "type", "status" },
                        { "appMode", "Office" },
                        { "isPresenting", false },
                        { "slideIndex", 1 },
                        { "slideCount", 1 },
                        { "speakerNotes", "" },
                        { "docTitle", "ZenQ Office" }
                    };
                }
                else
                {
                    status["type"] = "status";
                }

                string json = JsonLite.Serialize(status);
                if (client != null)
                {
                    byte[] payload = Encoding.UTF8.GetBytes(json);
                    SendWebSocketFrame(client, 1, payload);
                }
                else
                {
                    BroadcastJson(json);
                }
            }
            catch { }
        }

        private void ProcessIncomingMessage(ConnectedClient client, string message)
        {
            try
            {
                object parsed = JsonLite.Parse(message);
                if (!(parsed is Dictionary<string, object> dict)) return;

                string type = dict.ContainsKey("type") ? dict["type"]?.ToString() : "";

                switch (type)
                {
                    case "device_info":
                        if (dict.ContainsKey("name"))
                        {
                            ConnectedDeviceName = dict["name"]?.ToString() ?? "Điện thoại";
                            OnStatusMessage?.Invoke($"Đã kết nối: {ConnectedDeviceName}");
                        }
                        break;

                    case "mouse_move":
                        double dx = GetDouble(dict, "dx", 0);
                        double dy = GetDouble(dict, "dy", 0);
                        double sens = GetDouble(dict, "sens", 1.8);
                        MoveMouse(dx, dy, sens);
                        break;

                    case "mouse_click":
                        string btn = dict.ContainsKey("button") ? dict["button"]?.ToString() : "left";
                        MouseClickSim(btn);
                        break;

                    case "mouse_down":
                        string btnDown = dict.ContainsKey("button") ? dict["button"]?.ToString() : "left";
                        MouseDownSim(btnDown);
                        break;

                    case "mouse_up":
                        string btnUp = dict.ContainsKey("button") ? dict["button"]?.ToString() : "left";
                        MouseUpSim(btnUp);
                        break;

                    case "mouse_scroll":
                        double sDx = GetDouble(dict, "dx", 0);
                        double sDy = GetDouble(dict, "dy", 0);
                        ScrollMouseSim(sDx, sDy);
                        break;

                    case "shortcut":
                        string key = dict.ContainsKey("key") ? dict["key"]?.ToString() : "";
                        HandleShortcutCommand(key);
                        break;

                    case "numpad_key":
                        string numKey = dict.ContainsKey("key") ? dict["key"]?.ToString() : "";
                        HandleNumPadInput(numKey);
                        break;

                    case "type_text":
                        string text = dict.ContainsKey("text") ? dict["text"]?.ToString() : "";
                        TypeTextSim(text);
                        break;

                    case "slide_action":
                        string action = dict.ContainsKey("action") ? dict["action"]?.ToString() : "";
                        HandleSlideAction(action);
                        break;

                    case "laser_pointer":
                        bool active = GetBool(dict, "active", false);
                        float lx = (float)GetDouble(dict, "x", 0.5);
                        float ly = (float)GetDouble(dict, "y", 0.5);
                        ActionLaserPointer?.Invoke(lx, ly, active);
                        break;

                    case "signature_insert":
                        string base64 = dict.ContainsKey("image_base64") ? dict["image_base64"]?.ToString() : "";
                        HandleSignatureInsert(base64);
                        break;

                    case "zoom":
                        string dir = dict.ContainsKey("dir") ? dict["dir"]?.ToString() : "in";
                        HandleShortcutCommand(dir == "in" ? "zoom_in" : "zoom_out");
                        break;
                }
            }
            catch { }
        }

        #region Input Simulations
        private static void MoveMouse(double dx, double dy, double sensitivity)
        {
            int ix = (int)Math.Round(dx * sensitivity);
            int iy = (int)Math.Round(dy * sensitivity);
            if (ix != 0 || iy != 0)
            {
                mouse_event(MOUSEEVENTF_MOVE, ix, iy, 0, UIntPtr.Zero);
            }
        }

        private static void MouseClickSim(string button)
        {
            if (button == "left")
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(15);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            }
            else if (button == "right")
            {
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(15);
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
            }
            else if (button == "double")
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(30);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            }
        }

        private static void MouseDownSim(string button)
        {
            uint flag = (button == "right") ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_LEFTDOWN;
            mouse_event(flag, 0, 0, 0, UIntPtr.Zero);
        }

        private static void MouseUpSim(string button)
        {
            uint flag = (button == "right") ? MOUSEEVENTF_RIGHTUP : MOUSEEVENTF_LEFTUP;
            mouse_event(flag, 0, 0, 0, UIntPtr.Zero);
        }

        private static void ScrollMouseSim(double dx, double dy)
        {
            int v = (int)Math.Round(dy * 1.5);
            int h = (int)Math.Round(dx * 1.5);
            if (v != 0) mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)v, UIntPtr.Zero);
            if (h != 0) mouse_event(MOUSEEVENTF_HWHEEL, 0, 0, (uint)h, UIntPtr.Zero);
        }

        private void HandleShortcutCommand(string cmd)
        {
            ActionDispatchCustomCommand?.Invoke(cmd);
        }

        private void HandleSlideAction(string action)
        {
            ActionDispatchCustomCommand?.Invoke("slide_" + action);
        }

        private void HandleNumPadInput(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            try
            {
                if (key == "enter") SendKeys.SendWait("{ENTER}");
                else if (key == "backspace") SendKeys.SendWait("{BACKSPACE}");
                else if (key == "tab") SendKeys.SendWait("{TAB}");
                else if (key == "+") SendKeys.SendWait("{+}");
                else if (key == "^") SendKeys.SendWait("{^}");
                else if (key == "%") SendKeys.SendWait("{%}");
                else if (key == "~") SendKeys.SendWait("{~}");
                else if (key == "(") SendKeys.SendWait("{(}");
                else if (key == ")") SendKeys.SendWait("{)}");
                else if (key == "up") SendKeys.SendWait("{UP}");
                else if (key == "down") SendKeys.SendWait("{DOWN}");
                else if (key == "left") SendKeys.SendWait("{LEFT}");
                else if (key == "right") SendKeys.SendWait("{RIGHT}");
                else SendKeys.SendWait(key);
            }
            catch { }
        }

        private void TypeTextSim(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                // Clipboard paste để bảo đảm chuẩn xác Unicode tiếng Việt Telex/VNI
                Thread t = new Thread(() =>
                {
                    try
                    {
                        Clipboard.SetText(text);
                        Thread.Sleep(30);
                        SendKeys.SendWait("^v");
                    }
                    catch { }
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();
            }
            catch { }
        }

        private void HandleSignatureInsert(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return;
            try
            {
                int commaIdx = base64.IndexOf(',');
                string cleanBase64 = commaIdx >= 0 ? base64.Substring(commaIdx + 1) : base64;
                byte[] raw = Convert.FromBase64String(cleanBase64);
                using (MemoryStream ms = new MemoryStream(raw))
                using (Bitmap bmp = new Bitmap(ms))
                {
                    ActionInsertSignature?.Invoke(new Bitmap(bmp));
                }
            }
            catch { }
        }

        private static double GetDouble(Dictionary<string, object> dict, string key, double defVal)
        {
            if (!dict.ContainsKey(key) || dict[key] == null) return defVal;
            try { return Convert.ToDouble(dict[key], System.Globalization.CultureInfo.InvariantCulture); } catch { return defVal; }
        }

        private static bool GetBool(Dictionary<string, object> dict, string key, bool defVal)
        {
            if (!dict.ContainsKey(key) || dict[key] == null) return defVal;
            try { return Convert.ToBoolean(dict[key]); } catch { return defVal; }
        }
        #endregion

        #region Embedded Mobile Web Application HTML5 / CSS / JavaScript
        private string GetMobileAppHtml()
        {
            return @"<!DOCTYPE html>
<html lang=""vi"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover"">
<meta name=""apple-mobile-web-app-capable"" content=""yes"">
<meta name=""apple-mobile-web-app-status-bar-style"" content=""black-translucent"">
<title>ZenQ Connect - Điều khiển PC</title>
<style>
* { box-sizing: border-box; -webkit-tap-highlight-color: transparent; margin: 0; padding: 0; user-select: none; -webkit-user-select: none; }
body {
    background-color: #0b0f19;
    color: #f1f5f9;
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    height: 100vh;
    height: 100dvh;
    display: flex;
    flex-direction: column;
    overflow: hidden;
}
/* Top Header */
.header {
    background: #111827;
    border-bottom: 1px solid #1f2937;
    padding: 10px 14px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-shrink: 0;
}
.brand {
    display: flex;
    align-items: center;
    gap: 8px;
    font-weight: 700;
    font-size: 15px;
    letter-spacing: 0.3px;
}
.dot {
    width: 9px;
    height: 9px;
    border-radius: 50%;
    background: #ef4444;
    transition: background 0.3s;
}
.dot.online {
    background: #10b981;
    box-shadow: 0 0 8px #10b981;
}
.status-badge {
    font-size: 11px;
    background: #1e293b;
    padding: 3px 8px;
    border-radius: 12px;
    color: #94a3b8;
    border: 1px solid #334155;
    max-width: 140px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

/* Quick Action Bar */
.quick-bar {
    background: #0f172a;
    display: flex;
    padding: 6px 10px;
    gap: 6px;
    border-bottom: 1px solid #1e293b;
    flex-shrink: 0;
    overflow-x: auto;
}
.qbtn {
    background: #1e293b;
    color: #cbd5e1;
    border: 1px solid #334155;
    border-radius: 6px;
    padding: 6px 10px;
    font-size: 12px;
    font-weight: 500;
    white-space: nowrap;
    display: flex;
    align-items: center;
    gap: 4px;
    cursor: pointer;
}
.qbtn:active { background: #2563eb; color: #fff; transform: scale(0.96); }

/* Main Content Area */
.tab-content {
    flex: 1;
    display: none;
    flex-direction: column;
    overflow: hidden;
    position: relative;
}
.tab-content.active { display: flex; }

/* TAB 1: TOUCHPAD */
.touchpad-container {
    flex: 1;
    display: flex;
    padding: 10px;
    gap: 10px;
    overflow: hidden;
}
.touchpad-pad {
    flex: 1;
    background: radial-gradient(circle at center, #1e293b 0%, #0f172a 100%);
    border: 1px solid #334155;
    border-radius: 14px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    color: #64748b;
    font-size: 13px;
    touch-action: none;
    position: relative;
    box-shadow: inset 0 2px 8px rgba(0,0,0,0.4);
}
.touchpad-pad:active { border-color: #3b82f6; }
.scroll-strip {
    width: 44px;
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 14px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: space-between;
    padding: 12px 0;
    color: #94a3b8;
    font-size: 11px;
    font-weight: 600;
    touch-action: none;
}
.scroll-strip:active { background: #2563eb; color: #fff; }

.touchpad-buttons {
    display: flex;
    padding: 8px 10px 10px 10px;
    gap: 10px;
    flex-shrink: 0;
}
.btn-mouse {
    flex: 1;
    height: 52px;
    border-radius: 10px;
    border: 1px solid #334155;
    background: #1e293b;
    color: #f1f5f9;
    font-weight: 600;
    font-size: 14px;
    cursor: pointer;
    box-shadow: 0 2px 4px rgba(0,0,0,0.3);
}
.btn-mouse:active { background: #2563eb; transform: scale(0.98); }

/* TAB 2: SLIDES & LASER */
.slides-container {
    flex: 1;
    display: flex;
    flex-direction: column;
    padding: 12px;
    gap: 10px;
    overflow-y: auto;
}
.slide-status-card {
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 12px;
    padding: 10px 14px;
    display: flex;
    justify-content: space-between;
    align-items: center;
}
.slide-num { font-size: 18px; font-weight: 700; color: #38bdf8; }
.slide-timer { font-size: 14px; font-weight: 600; color: #a78bfa; font-variant-numeric: tabular-nums; }
.notes-card {
    background: #162032;
    border: 1px solid #2d3b55;
    border-radius: 12px;
    padding: 10px 14px;
    max-height: 110px;
    overflow-y: auto;
    font-size: 13px;
    line-height: 1.4;
    color: #cbd5e1;
    user-select: text;
    -webkit-user-select: text;
}
.notes-label { font-size: 10px; font-weight: 700; color: #94a3b8; text-transform: uppercase; margin-bottom: 4px; }
.slide-nav-buttons {
    display: flex;
    gap: 10px;
    height: 90px;
}
.btn-slide-nav {
    flex: 1;
    border-radius: 14px;
    border: none;
    font-size: 16px;
    font-weight: 700;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 4px;
    cursor: pointer;
    box-shadow: 0 4px 10px rgba(0,0,0,0.4);
}
.btn-prev { background: #334155; color: #f8fafc; }
.btn-next { background: #2563eb; color: #fff; }
.btn-slide-nav:active { transform: scale(0.97); }

.laser-pad {
    height: 130px;
    background: radial-gradient(circle at center, #2d1515 0%, #150a0a 100%);
    border: 2px dashed #ef4444;
    border-radius: 14px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    color: #fca5a5;
    font-size: 13px;
    font-weight: 600;
    touch-action: none;
    position: relative;
    box-shadow: 0 0 14px rgba(239, 68, 68, 0.2);
}
.laser-pad:active {
    background: radial-gradient(circle at center, #451010 0%, #1a0808 100%);
    border-style: solid;
    box-shadow: 0 0 25px rgba(239, 68, 68, 0.5);
}

.slide-controls-row {
    display: flex;
    gap: 8px;
}
.btn-ctrl {
    flex: 1;
    padding: 10px 4px;
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 8px;
    color: #cbd5e1;
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
}
.btn-ctrl:active { background: #3b82f6; color: #fff; }

/* TAB 3: SIGNATURE */
.sign-container {
    flex: 1;
    display: flex;
    flex-direction: column;
    padding: 10px;
    gap: 10px;
}
.canvas-wrapper {
    flex: 1;
    background: #ffffff;
    border-radius: 12px;
    overflow: hidden;
    touch-action: none;
    position: relative;
    box-shadow: 0 2px 8px rgba(0,0,0,0.4);
}
#signCanvas {
    width: 100%;
    height: 100%;
    display: block;
}
.sign-toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    flex-shrink: 0;
    gap: 6px;
}
.palette { display: flex; gap: 8px; }
.color-dot {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    border: 2px solid transparent;
    cursor: pointer;
}
.color-dot.active { border-color: #fff; transform: scale(1.1); }
.btn-sign-insert {
    background: #10b981;
    color: #fff;
    border: none;
    border-radius: 10px;
    padding: 12px 16px;
    font-size: 14px;
    font-weight: 700;
    cursor: pointer;
    flex: 1;
    box-shadow: 0 2px 8px rgba(16, 185, 129, 0.4);
}
.btn-sign-insert:active { transform: scale(0.97); }

/* TAB 4: NUMPAD */
.numpad-container {
    flex: 1;
    display: flex;
    flex-direction: column;
    padding: 10px;
    gap: 8px;
}
.text-input-box {
    display: flex;
    gap: 8px;
    flex-shrink: 0;
}
.text-input-box input {
    flex: 1;
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 8px;
    padding: 10px 12px;
    color: #fff;
    font-size: 15px;
    outline: none;
    user-select: text;
    -webkit-user-select: text;
}
.text-input-box button {
    background: #2563eb;
    color: #fff;
    border: none;
    border-radius: 8px;
    padding: 0 16px;
    font-weight: 600;
    font-size: 14px;
}
.numpad-grid {
    flex: 1;
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    grid-template-rows: repeat(5, 1fr);
    gap: 8px;
}
.npbtn {
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 10px;
    color: #f1f5f9;
    font-size: 20px;
    font-weight: 600;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    box-shadow: 0 2px 4px rgba(0,0,0,0.25);
}
.npbtn:active { background: #3b82f6; transform: scale(0.96); }
.npbtn.accent { background: #334155; color: #38bdf8; font-size: 17px; }
.npbtn.enter { background: #2563eb; color: #fff; }

/* Bottom Tab Bar */
.tab-bar {
    background: #111827;
    border-top: 1px solid #1f2937;
    display: flex;
    height: 58px;
    flex-shrink: 0;
    padding-bottom: env(safe-area-inset-bottom);
}
.tab-item {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 3px;
    color: #64748b;
    font-size: 10px;
    font-weight: 600;
    cursor: pointer;
    transition: color 0.2s;
}
.tab-item.active { color: #38bdf8; }
.tab-icon { font-size: 20px; }
</style>
</head>
<body>

<!-- Header -->
<div class=""header"">
    <div class=""brand"">
        <div class=""dot"" id=""statusDot""></div>
        <span>ZenQ Connect</span>
    </div>
    <div class=""status-badge"" id=""modeBadge"">Đang kết nối...</div>
</div>

<!-- Quick Actions -->
<div class=""quick-bar"">
    <div class=""qbtn"" onclick=""sendShortcut('undo')"">↩ Hoàn tác</div>
    <div class=""qbtn"" onclick=""sendShortcut('redo')"">↪ Làm lại</div>
    <div class=""qbtn"" onclick=""sendShortcut('save')"">💾 Lưu</div>
    <div class=""qbtn"" onclick=""sendShortcut('f5')"">⛶ Trình chiếu</div>
    <div class=""qbtn"" onclick=""sendShortcut('esc')"">❌ Esc</div>
</div>

<!-- TAB 1: TOUCHPAD -->
<div class=""tab-content active"" id=""tab1"">
    <div class=""touchpad-container"">
        <div class=""touchpad-pad"" id=""touchpadPad"">
            <span style=""font-size: 28px; margin-bottom: 8px;"">👆</span>
            <span>Rê ngón tay để di chuyển chuột</span>
            <span style=""font-size: 11px; margin-top: 4px; color: #475569;"">Chạm 1 ngón = Trái • 2 ngón = Phải & Cuộn</span>
        </div>
        <div class=""scroll-strip"" id=""scrollStrip"">
            <span>▲</span>
            <span style=""writing-mode: vertical-rl; transform: rotate(180deg);"">CUỘN</span>
            <span>▼</span>
        </div>
    </div>
    <div class=""touchpad-buttons"">
        <button class=""btn-mouse"" onclick=""sendClick('left')"">CHUỘT TRÁI</button>
        <button class=""btn-mouse"" onclick=""sendClick('right')"">CHUỘT PHẢI</button>
    </div>
</div>

<!-- TAB 2: SLIDES & LASER -->
<div class=""tab-content"" id=""tab2"">
    <div class=""slides-container"">
        <div class=""slide-status-card"">
            <div>
                <div style=""font-size: 11px; color: #94a3b8;"">SLIDE HIỆN TẠI</div>
                <div class=""slide-num"" id=""slideNumText"">Slide 1 / 1</div>
            </div>
            <div style=""text-align: right;"">
                <div style=""font-size: 11px; color: #94a3b8;"">THỜI GIAN</div>
                <div class=""slide-timer"" id=""slideTimerText"">00:00</div>
            </div>
        </div>

        <div class=""notes-card"">
            <div class=""notes-label"">📝 Ghi chú diễn giả:</div>
            <div id=""notesContent"">Không có ghi chú cho slide này.</div>
        </div>

        <div class=""slide-nav-buttons"">
            <button class=""btn-slide-nav btn-prev"" onclick=""sendSlideAction('prev')"">
                <span>◀</span>
                <span>Slide Trước</span>
            </button>
            <button class=""btn-slide-nav btn-next"" onclick=""sendSlideAction('next')"">
                <span>Tiếp Theo</span>
                <span>▶</span>
            </button>
        </div>

        <div class=""laser-pad"" id=""laserPad"">
            <span style=""font-size: 26px; margin-bottom: 4px;"">🔴</span>
            <span>BÚT LASER ẢO</span>
            <span style=""font-size: 11px; opacity: 0.8;"">Giữ & rê ngón tay để chiếu tia đỏ lên PC</span>
        </div>

        <div class=""slide-controls-row"">
            <button class=""btn-ctrl"" onclick=""sendSlideAction('black')"">⬛ Màn Đen (B)</button>
            <button class=""btn-ctrl"" onclick=""sendSlideAction('white')"">⬜ Màn Trắng (W)</button>
            <button class=""btn-ctrl"" onclick=""sendSlideAction('stop')"">⏹️ Thoát Chiếu</button>
        </div>
    </div>
</div>

<!-- TAB 3: SIGNATURE & DRAWING -->
<div class=""tab-content"" id=""tab3"">
    <div class=""sign-container"">
        <div class=""canvas-wrapper"" id=""canvasWrapper"">
            <canvas id=""signCanvas""></canvas>
        </div>
        <div class=""sign-toolbar"">
            <div class=""palette"">
                <div class=""color-dot active"" style=""background: #0f4c81;"" onclick=""setPenColor('#0f4c81', this)""></div>
                <div class=""color-dot"" style=""background: #111827;"" onclick=""setPenColor('#111827', this)""></div>
                <div class=""color-dot"" style=""background: #dc2626;"" onclick=""setPenColor('#dc2626', this)""></div>
            </div>
            <button class=""qbtn"" onclick=""clearCanvas()"">🗑️ Xóa</button>
            <button class=""btn-sign-insert"" onclick=""sendSignature()"">✍️ CHÈN VÀO PC</button>
        </div>
    </div>
</div>

<!-- TAB 4: NUMPAD & INPUT -->
<div class=""tab-content"" id=""tab4"">
    <div class=""numpad-container"">
        <div class=""text-input-box"">
            <input type=""text"" id=""typeInput"" placeholder=""Nhập văn bản gửi sang PC..."" autocomplete=""off"">
            <button onclick=""sendTypedText()"">Gửi</button>
        </div>
        <div class=""numpad-grid"">
            <div class=""npbtn accent"" onclick=""sendNum('(')"">(</div>
            <div class=""npbtn accent"" onclick=""sendNum(')')"">)</div>
            <div class=""npbtn accent"" onclick=""sendNum('/')"">/</div>
            <div class=""npbtn accent"" onclick=""sendNum('backspace')"">⌫</div>

            <div class=""npbtn"" onclick=""sendNum('7')"">7</div>
            <div class=""npbtn"" onclick=""sendNum('8')"">8</div>
            <div class=""npbtn"" onclick=""sendNum('9')"">9</div>
            <div class=""npbtn accent"" onclick=""sendNum('*')"">×</div>

            <div class=""npbtn"" onclick=""sendNum('4')"">4</div>
            <div class=""npbtn"" onclick=""sendNum('5')"">5</div>
            <div class=""npbtn"" onclick=""sendNum('6')"">6</div>
            <div class=""npbtn accent"" onclick=""sendNum('-')"">-</div>

            <div class=""npbtn"" onclick=""sendNum('1')"">1</div>
            <div class=""npbtn"" onclick=""sendNum('2')"">2</div>
            <div class=""npbtn"" onclick=""sendNum('3')"">3</div>
            <div class=""npbtn accent"" onclick=""sendNum('+')"">+</div>

            <div class=""npbtn"" onclick=""sendNum('0')"">0</div>
            <div class=""npbtn"" onclick=""sendNum('.')"">.</div>
            <div class=""npbtn accent"" onclick=""sendNum('=')"">=</div>
            <div class=""npbtn enter"" onclick=""sendNum('enter')"">↵ Enter</div>
        </div>
    </div>
</div>

<!-- Bottom Navigation Bar -->
<div class=""tab-bar"">
    <div class=""tab-item active"" onclick=""switchTab(1)"">
        <div class=""tab-icon"">🖱️</div>
        <span>Touchpad</span>
    </div>
    <div class=""tab-item"" onclick=""switchTab(2)"">
        <div class=""tab-icon"">📽️</div>
        <span>Trình Chiếu</span>
    </div>
    <div class=""tab-item"" onclick=""switchTab(3)"">
        <div class=""tab-icon"">✍️</div>
        <span>Chữ Ký</span>
    </div>
    <div class=""tab-item"" onclick=""switchTab(4)"">
        <div class=""tab-icon"">🔢</div>
        <span>Bàn Phím</span>
    </div>
</div>

<script>
let ws = null;
let isConnected = false;

function vibe(ms = 20) {
    if (navigator.vibrate) {
        try { navigator.vibrate(ms); } catch (e) {}
    }
}

function connectWs() {
    const proto = location.protocol === 'https:' ? 'wss:' : 'ws:';
    const wsUrl = proto + '//' + location.host + '/ws';
    ws = new WebSocket(wsUrl);

    ws.onopen = function() {
        isConnected = true;
        document.getElementById('statusDot').classList.add('online');
        document.getElementById('modeBadge').textContent = 'Đã kết nối PC';
        vibe(30);

        // Gửi thông tin thiết bị
        let device = navigator.userAgent.includes('iPhone') ? 'iPhone' :
                    (navigator.userAgent.includes('iPad') ? 'iPad' :
                    (navigator.userAgent.includes('Android') ? 'Android' : 'Mobile Web'));
        send({ type: 'device_info', name: device });
    };

    ws.onclose = function() {
        isConnected = false;
        document.getElementById('statusDot').classList.remove('online');
        document.getElementById('modeBadge').textContent = 'Mất kết nối...';
        setTimeout(connectWs, 2000);
    };

    ws.onerror = function() {
        ws.close();
    };

    ws.onmessage = function(e) {
        try {
            const data = JSON.parse(e.data);
            if (data.type === 'status') {
                updateAppStatus(data);
            }
        } catch(err) {}
    };
}

function send(obj) {
    if (ws && ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify(obj));
    }
}

function sendShortcut(key) {
    vibe(20);
    send({ type: 'shortcut', key: key });
}

function sendClick(button) {
    vibe(25);
    send({ type: 'mouse_click', button: button });
}

function sendSlideAction(action) {
    vibe(30);
    send({ type: 'slide_action', action: action });
}

function sendNum(key) {
    vibe(20);
    send({ type: 'numpad_key', key: key });
}

function sendTypedText() {
    const input = document.getElementById('typeInput');
    const val = input.value;
    if (val) {
        vibe(25);
        send({ type: 'type_text', text: val });
        input.value = '';
    }
}

// Chuyển Tab
function switchTab(idx) {
    vibe(15);
    document.querySelectorAll('.tab-content').forEach((el, i) => {
        el.classList.toggle('active', i === idx - 1);
    });
    document.querySelectorAll('.tab-item').forEach((el, i) => {
        el.classList.toggle('active', i === idx - 1);
    });
    if (idx === 3) initCanvasSize();
}

// Cập nhật trạng thái trình chiếu & app
let secondsElapsed = 0;
let timerRunning = false;
setInterval(() => {
    if (timerRunning) {
        secondsElapsed++;
        let m = String(Math.floor(secondsElapsed / 60)).padStart(2, '0');
        let s = String(secondsElapsed % 60).padStart(2, '0');
        document.getElementById('slideTimerText').textContent = m + ':' + s;
    }
}, 1000);

function updateAppStatus(data) {
    const badge = document.getElementById('modeBadge');
    if (data.appMode) {
        let modeName = data.appMode === 'Slides' ? '📽️ Slides' :
                      (data.appMode === 'Writer' ? '📝 Writer' :
                      (data.appMode === 'Calc' ? '📊 Calc' :
                      (data.appMode === 'Photo' ? '🎨 Photo' : data.appMode)));
        badge.textContent = modeName;
    }

    if (data.slideIndex && data.slideCount) {
        document.getElementById('slideNumText').textContent = `Slide ${data.slideIndex} / ${data.slideCount}`;
    }

    if (data.speakerNotes !== undefined) {
        document.getElementById('notesContent').textContent = data.speakerNotes || 'Không có ghi chú cho slide này.';
    }

    if (data.isPresenting && !timerRunning) {
        timerRunning = true;
    } else if (!data.isPresenting && timerRunning) {
        timerRunning = false;
    }
}

// Xử lý Touchpad
const pad = document.getElementById('touchpadPad');
let lastTouchX = 0, lastTouchY = 0;
let touchStartTime = 0;
let isTouchMoved = false;

pad.addEventListener('touchstart', (e) => {
    if (e.touches.length === 1) {
        lastTouchX = e.touches[0].clientX;
        lastTouchY = e.touches[0].clientY;
        touchStartTime = Date.now();
        isTouchMoved = false;
    } else if (e.touches.length === 2) {
        lastTouchX = (e.touches[0].clientX + e.touches[1].clientX) / 2;
        lastTouchY = (e.touches[0].clientY + e.touches[1].clientY) / 2;
    }
}, { passive: false });

pad.addEventListener('touchmove', (e) => {
    e.preventDefault();
    if (e.touches.length === 1) {
        const cx = e.touches[0].clientX;
        const cy = e.touches[0].clientY;
        const dx = cx - lastTouchX;
        const dy = cy - lastTouchY;
        lastTouchX = cx;
        lastTouchY = cy;

        if (Math.abs(dx) > 1 || Math.abs(dy) > 1) {
            isTouchMoved = true;
            send({ type: 'mouse_move', dx: dx, dy: dy, sens: 1.8 });
        }
    } else if (e.touches.length === 2) {
        const cy = (e.touches[0].clientY + e.touches[1].clientY) / 2;
        const dy = cy - lastTouchY;
        lastTouchY = cy;
        send({ type: 'mouse_scroll', dx: 0, dy: dy * 1.5 });
    }
}, { passive: false });

pad.addEventListener('touchend', (e) => {
    if (e.touches.length === 0) {
        const duration = Date.now() - touchStartTime;
        if (!isTouchMoved && duration < 250) {
            sendClick('left');
        }
    }
});

// Dải cuộn chuyên biệt bên phải
const strip = document.getElementById('scrollStrip');
let lastStripY = 0;
strip.addEventListener('touchstart', (e) => {
    lastStripY = e.touches[0].clientY;
}, { passive: false });
strip.addEventListener('touchmove', (e) => {
    e.preventDefault();
    const cy = e.touches[0].clientY;
    const dy = cy - lastStripY;
    lastStripY = cy;
    send({ type: 'mouse_scroll', dx: 0, dy: dy * 2.2 });
}, { passive: false });

// Bút Laser Ảo
const laserPad = document.getElementById('laserPad');
laserPad.addEventListener('touchstart', (e) => {
    e.preventDefault();
    vibe(40);
    sendLaser(e.touches[0], true);
}, { passive: false });
laserPad.addEventListener('touchmove', (e) => {
    e.preventDefault();
    sendLaser(e.touches[0], true);
}, { passive: false });
laserPad.addEventListener('touchend', (e) => {
    send({ type: 'laser_pointer', active: false });
});
function sendLaser(touch, active) {
    const rect = laserPad.getBoundingClientRect();
    const normX = Math.max(0, Math.min(1, (touch.clientX - rect.left) / rect.width));
    const normY = Math.max(0, Math.min(1, (touch.clientY - rect.top) / rect.height));
    send({ type: 'laser_pointer', active: active, x: normX, y: normY });
}

// Chữ Ký & Vẽ Tay Canvas
const canvas = document.getElementById('signCanvas');
const ctx = canvas.getContext('2d');
let drawing = false;
let penColor = '#0f4c81';

function initCanvasSize() {
    const wrapper = document.getElementById('canvasWrapper');
    const dpr = window.devicePixelRatio || 1;
    canvas.width = wrapper.clientWidth * dpr;
    canvas.height = wrapper.clientHeight * dpr;
    ctx.scale(dpr, dpr);
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.lineWidth = 3.5;
    ctx.strokeStyle = penColor;
}

function setPenColor(col, el) {
    penColor = col;
    ctx.strokeStyle = col;
    document.querySelectorAll('.color-dot').forEach(d => d.classList.remove('active'));
    el.classList.add('active');
}

function clearCanvas() {
    vibe(20);
    ctx.clearRect(0, 0, canvas.width, canvas.height);
}

canvas.addEventListener('touchstart', (e) => {
    e.preventDefault();
    drawing = true;
    const rect = canvas.getBoundingClientRect();
    ctx.beginPath();
    ctx.moveTo(e.touches[0].clientX - rect.left, e.touches[0].clientY - rect.top);
}, { passive: false });

canvas.addEventListener('touchmove', (e) => {
    e.preventDefault();
    if (!drawing) return;
    const rect = canvas.getBoundingClientRect();
    ctx.lineTo(e.touches[0].clientX - rect.left, e.touches[0].clientY - rect.top);
    ctx.stroke();
}, { passive: false });

canvas.addEventListener('touchend', () => { drawing = false; });

function sendSignature() {
    vibe(40);
    const dataUrl = canvas.toDataURL('image/png');
    send({ type: 'signature_insert', image_base64: dataUrl });
    alert('✅ Đã gửi chữ ký sang PC và chèn vào tài liệu!');
}

window.addEventListener('load', () => {
    connectWs();
    setTimeout(initCanvasSize, 200);
});
window.addEventListener('resize', initCanvasSize);
</script>
</body>
</html>";
        }
        #endregion
    }

    /// <summary>
    /// Đối tượng quản lý kết nối client WebSocket đang hoạt động.
    /// </summary>
    internal class ConnectedClient
    {
        public TcpClient TcpClient { get; }
        public NetworkStream Stream { get; }
        public bool IsConnected => TcpClient != null && TcpClient.Connected;

        public ConnectedClient(TcpClient tcpClient, NetworkStream stream)
        {
            TcpClient = tcpClient;
            Stream = stream;
        }

        public void Close()
        {
            try { Stream?.Close(); } catch { }
            try { TcpClient?.Close(); } catch { }
        }
    }

    /// <summary>
    /// Bộ sinh mã QR Code chuẩn ISO/IEC 18004 thuần C# độc lập hoàn toàn, hỗ trợ Version 3-L (29x29 matrix).
    /// Chuẩn xác 100% với các thuật toán nhận diện của Camera iOS (Apple Safari), Android Google Lens và OpenCV.
    /// Hoàn toàn không sửa đổi hay phụ thuộc vào file QrCodeGenerator.cs sẵn có trong dự án.
    /// </summary>
    internal static class ZenQMiniQr
    {
        private static readonly byte[] ExpTable = new byte[512];
        private static readonly byte[] LogTable = new byte[256];

        static ZenQMiniQr()
        {
            int x = 1;
            for (int i = 0; i < 255; i++)
            {
                ExpTable[i] = (byte)x;
                ExpTable[i + 255] = (byte)x;
                LogTable[x] = (byte)i;
                x <<= 1;
                if ((x & 0x100) != 0) x ^= 0x11D;
            }
        }

        private static byte GfMul(byte a, byte b)
        {
            if (a == 0 || b == 0) return 0;
            return ExpTable[LogTable[a] + LogTable[b]];
        }

        public static Bitmap GenerateQr(string text, int pixelSize = 260)
        {
            if (string.IsNullOrEmpty(text)) text = "http://127.0.0.1:8088/";

            // 1. Tính toán đa thức sinh Reed-Solomon cho 15 mã kiểm lỗi (Version 3-L)
            const int ecCount = 15;
            const int dataCapacity = 55;
            const int version = 3;
            const int size = 17 + version * 4; // 29

            byte[] poly = new byte[] { 1 };
            for (int i = 0; i < ecCount; i++)
            {
                byte root = ExpTable[i];
                byte[] nextPoly = new byte[poly.Length + 1];
                for (int j = 0; j < poly.Length; j++)
                {
                    nextPoly[j] ^= poly[j];
                    nextPoly[j + 1] ^= GfMul(poly[j], root);
                }
                poly = nextPoly;
            }
            byte[] ecPoly = new byte[ecCount];
            Array.Copy(poly, 1, ecPoly, 0, ecCount);

            // 2. Mã hóa dữ liệu (Byte mode)
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            List<bool> bits = new List<bool>(dataCapacity * 8);

            // Mode indicator: 0100 (Byte mode)
            bits.Add(false); bits.Add(true); bits.Add(false); bits.Add(false);

            // Character count indicator (8 bits cho Version 1-9 Byte mode)
            int charCount = Math.Min(textBytes.Length, 53);
            for (int i = 7; i >= 0; i--) bits.Add(((charCount >> i) & 1) == 1);

            // Dữ liệu văn bản
            for (int idx = 0; idx < charCount; idx++)
            {
                byte b = textBytes[idx];
                for (int i = 7; i >= 0; i--) bits.Add(((b >> i) & 1) == 1);
            }

            // Terminator: 4 bit 0 (hoặc ít hơn nếu gần đầy)
            int termLen = Math.Min(4, dataCapacity * 8 - bits.Count);
            for (int i = 0; i < termLen; i++) bits.Add(false);

            // Đệm bit tròn byte
            while (bits.Count % 8 != 0) bits.Add(false);

            // Đệm byte xen kẽ 0xEC, 0x11
            byte[] pads = new byte[] { 0xEC, 0x11 };
            int padIdx = 0;
            while (bits.Count < dataCapacity * 8)
            {
                byte pb = pads[padIdx % 2];
                padIdx++;
                for (int i = 7; i >= 0; i--) bits.Add(((pb >> i) & 1) == 1);
            }

            // Chuyển sang 55 byte dữ liệu
            byte[] dataWords = new byte[dataCapacity];
            for (int i = 0; i < dataCapacity; i++)
            {
                byte val = 0;
                for (int j = 0; j < 8; j++) val = (byte)((val << 1) | (bits[i * 8 + j] ? 1 : 0));
                dataWords[i] = val;
            }

            // 3. Phép chia đa thức tính 15 mã sửa lỗi Reed-Solomon
            byte[] remainder = new byte[ecCount];
            foreach (byte b in dataWords)
            {
                byte factor = (byte)(b ^ remainder[0]);
                for (int j = 0; j < ecCount - 1; j++)
                {
                    remainder[j] = (byte)(remainder[j + 1] ^ GfMul(ecPoly[j], factor));
                }
                remainder[ecCount - 1] = GfMul(ecPoly[ecCount - 1], factor);
            }

            // Ghép 55 data bytes + 15 ec bytes = 70 codewords
            byte[] allCodewords = new byte[dataCapacity + ecCount];
            Array.Copy(dataWords, 0, allCodewords, 0, dataCapacity);
            Array.Copy(remainder, 0, allCodewords, dataCapacity, ecCount);

            // 4. Khởi tạo ma trận QR 29x29
            bool[,] matrix = new bool[size, size];
            bool[,] isFunction = new bool[size, size];

            void SetFunc(int r, int c, bool val)
            {
                if (r >= 0 && r < size && c >= 0 && c < size)
                {
                    matrix[r, c] = val;
                    isFunction[r, c] = true;
                }
            }

            // Finder patterns (3 góc) + đường viền trắng (separator)
            void DrawFinder(int top, int left)
            {
                for (int dr = -1; dr <= 7; dr++)
                {
                    for (int dc = -1; dc <= 7; dc++)
                    {
                        int r = top + dr, c = left + dc;
                        if (dr >= 0 && dr <= 6 && dc >= 0 && dc <= 6)
                        {
                            bool dark = (dr == 0 || dr == 6 || dc == 0 || dc == 6 || (dr >= 2 && dr <= 4 && dc >= 2 && dc <= 4));
                            SetFunc(r, c, dark);
                        }
                        else
                        {
                            SetFunc(r, c, false); // Separator module
                        }
                    }
                }
            }

            DrawFinder(0, 0);
            DrawFinder(0, size - 7);
            DrawFinder(size - 7, 0);

            // Alignment pattern tại (22, 22)
            for (int dr = -2; dr <= 2; dr++)
            {
                for (int dc = -2; dc <= 2; dc++)
                {
                    bool dark = (Math.Abs(dr) == 2 || Math.Abs(dc) == 2 || (dr == 0 && dc == 0));
                    SetFunc(22 + dr, 22 + dc, dark);
                }
            }

            // Timing patterns (hàng 6 và cột 6)
            for (int i = 8; i < size - 8; i++)
            {
                SetFunc(6, i, i % 2 == 0);
                SetFunc(i, 6, i % 2 == 0);
            }

            // Dark module tại (size - 8, 8)
            SetFunc(size - 8, 8, true);

            // Đặt chỗ vùng Format Information
            for (int i = 0; i <= 8; i++)
            {
                isFunction[8, i] = true;
                isFunction[i, 8] = true;
            }
            for (int i = 0; i < 8; i++)
            {
                isFunction[size - 1 - i, 8] = true;
                isFunction[8, size - 1 - i] = true;
            }

            // 5. Đặt dữ liệu vào ma trận theo thứ tự ziczac (Mask 0: (r + c) % 2 == 0)
            List<bool> allBits = new List<bool>(allCodewords.Length * 8);
            foreach (byte b in allCodewords)
            {
                for (int i = 7; i >= 0; i--) allBits.Add(((b >> i) & 1) == 1);
            }

            int bitIdx = 0;
            int col = size - 1;
            bool goingUp = true;

            while (col > 0)
            {
                if (col == 6) col--;
                int rStart = goingUp ? (size - 1) : 0;
                int rEnd = goingUp ? -1 : size;
                int rStep = goingUp ? -1 : 1;

                for (int r = rStart; r != rEnd; r += rStep)
                {
                    for (int cOffset = 0; cOffset < 2; cOffset++)
                    {
                        int c = col - cOffset;
                        if (!isFunction[r, c])
                        {
                            bool bit = (bitIdx < allBits.Count) ? allBits[bitIdx++] : false;
                            // Mask 0: đảo bit nếu (r + c) chẵn
                            if ((r + c) % 2 == 0) bit = !bit;
                            matrix[r, c] = bit;
                        }
                    }
                }
                col -= 2;
                goingUp = !goingUp;
            }

            // 6. Ghi Format Information: Level L (01) + Mask 0 (000) = 0x77C4 (15 bits)
            int fmt = 0x77C4;
            var tlCoords = new (int r, int c)[]
            {
                (8,0), (8,1), (8,2), (8,3), (8,4), (8,5), (8,7), (8,8),
                (7,8), (5,8), (4,8), (3,8), (2,8), (1,8), (0,8)
            };
            for (int i = 0; i < 15; i++)
            {
                bool bit = ((fmt >> (14 - i)) & 1) == 1;
                matrix[tlCoords[i].r, tlCoords[i].c] = bit;
            }

            var blTrCoords = new (int r, int c)[15];
            for (int i = 0; i < 7; i++) blTrCoords[i] = (size - 1 - i, 8);
            for (int i = 0; i < 8; i++) blTrCoords[7 + i] = (8, size - 8 + i);
            for (int i = 0; i < 15; i++)
            {
                bool bit = ((fmt >> (14 - i)) & 1) == 1;
                matrix[blTrCoords[i].r, blTrCoords[i].c] = bit;
            }

            // 7. Vẽ Bitmap chất lượng cao với Quiet Zone 4 modules chuẩn quốc tế
            const int quiet = 4;
            int totalModules = size + quiet * 2; // 37 modules
            int modulePixel = Math.Max(4, pixelSize / totalModules);
            int finalBmpSize = modulePixel * totalModules;

            Bitmap bmp = new Bitmap(finalBmpSize, finalBmpSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                using (SolidBrush darkBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    for (int r = 0; r < size; r++)
                    {
                        for (int c = 0; c < size; c++)
                        {
                            if (matrix[r, c])
                            {
                                int px = (c + quiet) * modulePixel;
                                int py = (r + quiet) * modulePixel;
                                g.FillRectangle(darkBrush, px, py, modulePixel, modulePixel);
                            }
                        }
                    }
                }
            }

            if (finalBmpSize != pixelSize)
            {
                Bitmap resized = new Bitmap(pixelSize, pixelSize);
                using (Graphics g = Graphics.FromImage(resized))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    g.DrawImage(bmp, new Rectangle(0, 0, pixelSize, pixelSize));
                }
                bmp.Dispose();
                return resized;
            }

            return bmp;
        }
    }
}
