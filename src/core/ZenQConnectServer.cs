using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
    /// 2. Xem trực tiếp & chia sẻ màn hình PC trên điện thoại thời gian thực (Screen Mirroring) và chạm điều khiển.
    /// 3. Điều khiển trình chiếu Slide chuyên nghiệp (Next, Prev, Timer, Ghi chú diễn giả live, Màn hình đen/trắng).
    /// 4. Bút Laser ảo thời gian thực (chiếu điểm laser đỏ trên màn hình máy chiếu từ màn hình cảm ứng điện thoại).
    /// 5. Bàn vẽ chữ ký số & phác thảo (chèn tức thì 1-chạm vào văn bản Word/Writer, Slide PPTX, hoặc Bảng tính Calc).
    /// 6. Bàn phím số NumPad & Hộp nhập văn bản tiếng Việt từ xa.
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

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

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

        #region Screen Mirroring
        private static readonly object _screenCaptureLock = new object();
        private static byte[] _lastScreenBytes = null;
        private static DateTime _lastCaptureTime = DateTime.MinValue;

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public static byte[] CaptureScreenJpeg(int targetW = 960, int targetH = 540, long quality = 60L)
        {
            lock (_screenCaptureLock)
            {
                // Cache 35ms để phục vụ mượt mà ~20 FPS mà không làm nặng CPU
                if (_lastScreenBytes != null && (DateTime.UtcNow - _lastCaptureTime).TotalMilliseconds < 35)
                {
                    return _lastScreenBytes;
                }

                try
                {
                    Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
                    using (Bitmap bmpScreen = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppRgb))
                    {
                        using (Graphics gScreen = Graphics.FromImage(bmpScreen))
                        {
                            gScreen.CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size, CopyPixelOperation.SourceCopy);

                            // Vẽ con trỏ chuột thật của Windows lên ảnh
                            try
                            {
                                CURSORINFO pci;
                                pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                                if (GetCursorInfo(out pci) && pci.flags == 1 /* CURSOR_SHOWING */)
                                {
                                    using (Icon icon = Icon.FromHandle(pci.hCursor))
                                    {
                                        Point cursorPt = new Point(pci.ptScreenPos.x - screenBounds.X, pci.ptScreenPos.y - screenBounds.Y);
                                        gScreen.DrawIcon(icon, cursorPt.X, cursorPt.Y);
                                    }
                                }
                            }
                            catch { }
                        }

                        // Giữ tỉ lệ co giãn màn hình PC
                        int outW = targetW;
                        int outH = targetH;
                        if (outW <= 0 || outH <= 0)
                        {
                            outW = 960;
                            outH = 540;
                        }
                        float ratio = Math.Min((float)outW / screenBounds.Width, (float)outH / screenBounds.Height);
                        int finalW = Math.Max(100, (int)(screenBounds.Width * ratio));
                        int finalH = Math.Max(100, (int)(screenBounds.Height * ratio));

                        using (Bitmap scaledBmp = new Bitmap(finalW, finalH, PixelFormat.Format24bppRgb))
                        {
                            using (Graphics gScaled = Graphics.FromImage(scaledBmp))
                            {
                                gScaled.InterpolationMode = InterpolationMode.Bilinear;
                                gScaled.DrawImage(bmpScreen, 0, 0, finalW, finalH);
                            }

                            ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                            using (EncoderParameters encParams = new EncoderParameters(1))
                            {
                                encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    scaledBmp.Save(ms, jpgEncoder, encParams);
                                    _lastScreenBytes = ms.ToArray();
                                    _lastCaptureTime = DateTime.UtcNow;
                                    return _lastScreenBytes;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    return _lastScreenBytes;
                }
            }
        }
        #endregion

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

            // 2.5. Phục vụ hình ảnh chia sẻ màn hình PC trực tiếp: /screen.jpg
            if (path == "/screen.jpg" || path.StartsWith("/screen.jpg?"))
            {
                int targetW = 960;
                int targetH = 540;
                long quality = 60L;
                if (path.Contains("q=hd"))
                {
                    targetW = 1280;
                    targetH = 720;
                    quality = 72L;
                }
                else if (path.Contains("q=fast"))
                {
                    targetW = 720;
                    targetH = 405;
                    quality = 48L;
                }
                byte[] jpegBytes = CaptureScreenJpeg(targetW, targetH, quality);
                if (jpegBytes != null && jpegBytes.Length > 0)
                {
                    string header = "HTTP/1.1 200 OK\r\n" +
                                   "Content-Type: image/jpeg\r\n" +
                                   $"Content-Length: {jpegBytes.Length}\r\n" +
                                   "Cache-Control: no-cache, no-store, must-revalidate\r\n" +
                                   "Pragma: no-cache\r\n" +
                                   "Expires: 0\r\n" +
                                   "Access-Control-Allow-Origin: *\r\n" +
                                   "Connection: close\r\n\r\n";
                    byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                    await stream.WriteAsync(headerBytes, 0, headerBytes.Length, token);
                    await stream.WriteAsync(jpegBytes, 0, jpegBytes.Length, token);
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
                                 "Cache-Control: no-cache, no-store, must-revalidate\r\n" +
                                 "Pragma: no-cache\r\n" +
                                 "Expires: 0\r\n" +
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

                    case "screen_click":
                        double scX = GetDouble(dict, "x", 0.5);
                        double scY = GetDouble(dict, "y", 0.5);
                        string scBtn = dict.ContainsKey("button") ? dict["button"]?.ToString() : "left";
                        Rectangle pBounds = Screen.PrimaryScreen.Bounds;
                        int targetX = pBounds.X + (int)Math.Round(scX * pBounds.Width);
                        int targetY = pBounds.Y + (int)Math.Round(scY * pBounds.Height);
                        SetCursorPos(targetX, targetY);
                        Thread.Sleep(10);
                        MouseClickSim(scBtn);
                        break;

                    case "screen_move":
                        double smX = GetDouble(dict, "x", 0.5);
                        double smY = GetDouble(dict, "y", 0.5);
                        Rectangle smBounds = Screen.PrimaryScreen.Bounds;
                        SetCursorPos(smBounds.X + (int)Math.Round(smX * smBounds.Width), smBounds.Y + (int)Math.Round(smY * smBounds.Height));
                        break;

                    case "screen_mouse_down":
                        double smdX = GetDouble(dict, "x", 0.5);
                        double smdY = GetDouble(dict, "y", 0.5);
                        string smdBtn = dict.ContainsKey("button") ? dict["button"]?.ToString() : "left";
                        Rectangle smdBounds = Screen.PrimaryScreen.Bounds;
                        SetCursorPos(smdBounds.X + (int)Math.Round(smdX * smdBounds.Width), smdBounds.Y + (int)Math.Round(smdY * smdBounds.Height));
                        Thread.Sleep(10);
                        MouseDownSim(smdBtn);
                        break;

                    case "screen_mouse_up":
                        double smuX = GetDouble(dict, "x", 0.5);
                        double smuY = GetDouble(dict, "y", 0.5);
                        string smuBtn = dict.ContainsKey("button") ? dict["button"]?.ToString() : "left";
                        Rectangle smuBounds = Screen.PrimaryScreen.Bounds;
                        SetCursorPos(smuBounds.X + (int)Math.Round(smuX * smuBounds.Width), smuBounds.Y + (int)Math.Round(smuY * smuBounds.Height));
                        Thread.Sleep(10);
                        MouseUpSim(smuBtn);
                        break;

                    case "screen_scroll":
                        double scrDy = GetDouble(dict, "dy", 0);
                        ScrollMouseSim(0, scrDy);
                        break;

                    case "key_down":
                        string kdKey = dict.ContainsKey("key") ? dict["key"]?.ToString() : "";
                        SimulateKey(kdKey, isDown: true);
                        break;

                    case "key_up":
                        string kuKey = dict.ContainsKey("key") ? dict["key"]?.ToString() : "";
                        SimulateKey(kuKey, isDown: false);
                        break;

                    case "key_press":
                        string kpKey = dict.ContainsKey("key") ? dict["key"]?.ToString() : "";
                        SimulateKeyPress(kpKey);
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
            // Tinh chỉnh độ nhạy cuộn: giảm gia tốc và kẹp biên độ tối đa per-event
            // để tránh bị trôi đi quá xa khi cuộn trên các vùng có smooth animation
            int v = (int)Math.Round(dy * 0.85);
            int h = (int)Math.Round(dx * 0.85);
            v = Math.Max(-120, Math.Min(120, v));
            h = Math.Max(-120, Math.Min(120, h));
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

        private const byte VK_BACK = 0x08;
        private const byte VK_TAB = 0x09;
        private const byte VK_RETURN = 0x0D;
        private const byte VK_SHIFT = 0x10;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_MENU = 0x12; // Alt
        private const byte VK_ESCAPE = 0x1B;
        private const byte VK_SPACE = 0x20;
        private const byte VK_SNAPSHOT = 0x2C; // Print Screen
        private const byte VK_DELETE = 0x2E;
        private const byte VK_LWIN = 0x5B;
        private const byte VK_LEFT = 0x25;
        private const byte VK_UP = 0x26;
        private const byte VK_RIGHT = 0x27;
        private const byte VK_DOWN = 0x28;
        private const byte VK_F5 = 0x74;
        private const byte VK_F11 = 0x7A;

        private static byte GetVkCode(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;
            switch (key.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    return VK_CONTROL;
                case "alt":
                    return VK_MENU;
                case "shift":
                    return VK_SHIFT;
                case "win":
                case "windows":
                    return VK_LWIN;
                case "esc":
                case "escape":
                    return VK_ESCAPE;
                case "tab":
                    return VK_TAB;
                case "del":
                case "delete":
                    return VK_DELETE;
                case "enter":
                case "return":
                    return VK_RETURN;
                case "backspace":
                    return VK_BACK;
                case "space":
                    return VK_SPACE;
                case "prtscn":
                case "printscreen":
                case "screenshot":
                    return VK_SNAPSHOT;
                case "f5":
                    return VK_F5;
                case "f11":
                    return VK_F11;
                case "left":
                    return VK_LEFT;
                case "up":
                    return VK_UP;
                case "right":
                    return VK_RIGHT;
                case "down":
                    return VK_DOWN;
                default:
                    if (key.Length == 1)
                    {
                        char c = char.ToUpperInvariant(key[0]);
                        if (c >= 'A' && c <= 'Z') return (byte)c;
                        if (c >= '0' && c <= '9') return (byte)c;
                    }
                    return 0;
            }
        }

        private static void SimulateKey(string key, bool isDown)
        {
            byte vk = GetVkCode(key);
            if (vk != 0)
            {
                uint flags = isDown ? 0 : KEYEVENTF_KEYUP;
                keybd_event(vk, 0, flags, UIntPtr.Zero);
            }
        }

        private static void SimulateKeyPress(string key)
        {
            byte vk = GetVkCode(key);
            if (vk != 0)
            {
                keybd_event(vk, 0, 0, UIntPtr.Zero);
                Thread.Sleep(25);
                keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            else
            {
                try { SendKeys.SendWait(key); } catch { }
            }
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
<meta http-equiv=""Cache-Control"" content=""no-cache, no-store, must-revalidate"">
<meta http-equiv=""Pragma"" content=""no-cache"">
<meta http-equiv=""Expires"" content=""0"">
<title>ZenQ Connect - Điều khiển PC</title>
<style>
* { box-sizing: border-box; -webkit-tap-highlight-color: transparent; margin: 0; padding: 0; user-select: none; -webkit-user-select: none; }
html, body {
    width: 100%;
    height: 100%;
    height: 100dvh;
    margin: 0;
    padding: 0;
    overflow: hidden;
    position: fixed;
    top: 0; left: 0; right: 0; bottom: 0;
    display: flex;
    flex-direction: column;
    background-color: #0b0f19;
    color: #f1f5f9;
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    touch-action: pan-y;
}
/* Top Header */
.header {
    background: #111827;
    border-bottom: 1px solid #1f2937;
    padding: 0 12px;
    height: 42px;
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
    font-size: 14px;
    letter-spacing: 0.3px;
}
.dot {
    width: 8px;
    height: 8px;
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
    max-width: 150px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

/* Quick Action Bar */
.quick-bar {
    background: #0f172a;
    display: flex;
    height: 36px;
    padding: 4px 8px;
    gap: 6px;
    border-bottom: 1px solid #1e293b;
    flex-shrink: 0;
    overflow-x: auto;
    white-space: nowrap;
    -webkit-overflow-scrolling: touch;
    scrollbar-width: none;
}
.quick-bar::-webkit-scrollbar { display: none; }
.qbtn {
    background: #1e293b;
    color: #cbd5e1;
    border: 1px solid #334155;
    border-radius: 6px;
    padding: 0 9px;
    height: 26px;
    font-size: 11.5px;
    font-weight: 500;
    display: inline-flex;
    align-items: center;
    gap: 4px;
    cursor: pointer;
    flex-shrink: 0;
}
.qbtn:active { background: #2563eb; color: #fff; transform: scale(0.96); }

/* Main Content Area */
.tab-content {
    flex: 1 1 0%;
    min-height: 0;
    display: none;
    flex-direction: column;
    overflow: hidden;
    position: relative;
    width: 100%;
}
.tab-content.active { display: flex; }

/* TAB 1: TOUCHPAD */
.touchpad-container {
    flex: 1 1 0%;
    min-height: 0;
    display: flex;
    padding: 8px;
    gap: 8px;
    overflow: hidden;
}
.touchpad-pad {
    flex: 1 1 0%;
    min-height: 0;
    background: radial-gradient(circle at center, #1e293b 0%, #0f172a 100%);
    border: 1px solid #334155;
    border-radius: 12px;
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
    border-radius: 12px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: space-between;
    padding: 10px 0;
    color: #94a3b8;
    font-size: 11px;
    font-weight: 600;
    touch-action: none;
    flex-shrink: 0;
}
.scroll-strip:active { background: #2563eb; color: #fff; }

.touchpad-buttons {
    display: flex;
    padding: 6px 8px 8px 8px;
    gap: 8px;
    flex-shrink: 0;
}
.btn-mouse {
    flex: 1;
    height: 46px;
    border-radius: 10px;
    border: 1px solid #334155;
    background: #1e293b;
    color: #f1f5f9;
    font-weight: 600;
    font-size: 13px;
    cursor: pointer;
    box-shadow: 0 2px 4px rgba(0,0,0,0.3);
}
.btn-mouse:active { background: #2563eb; transform: scale(0.98); }

/* TAB 2: LIVE SCREEN MIRROR */
.screen-container {
    flex: 1 1 0%;
    min-height: 0;
    display: flex;
    flex-direction: column;
    background: #020617;
    overflow: hidden;
}
.screen-toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 5px 10px;
    background: #0f172a;
    border-bottom: 1px solid #1e293b;
    flex-shrink: 0;
}
.screen-stat {
    font-size: 11px;
    font-weight: 600;
    color: #10b981;
    display: flex;
    align-items: center;
    gap: 4px;
}
.sbtn {
    background: #1e293b;
    color: #cbd5e1;
    border: 1px solid #334155;
    border-radius: 6px;
    padding: 4px 8px;
    font-size: 11px;
    font-weight: 600;
    cursor: pointer;
}
.sbtn:active { background: #2563eb; color: #fff; }
.screen-viewport {
    flex: 1 1 0%;
    min-height: 0;
    position: relative;
    display: flex;
    align-items: center;
    justify-content: center;
    overflow: hidden;
    background: #000;
    touch-action: none;
}
#screenLiveImg {
    max-width: 100%;
    max-height: 100%;
    width: auto;
    height: auto;
    object-fit: contain;
    display: block;
    cursor: crosshair;
}
.screen-touch-hint {
    position: absolute;
    bottom: 6px;
    background: rgba(15, 23, 42, 0.85);
    backdrop-filter: blur(6px);
    padding: 4px 12px;
    border-radius: 12px;
    font-size: 11px;
    color: #cbd5e1;
    pointer-events: none;
    border: 1px solid rgba(255,255,255,0.1);
    box-shadow: 0 2px 6px rgba(0,0,0,0.5);
    text-align: center;
    max-width: 95%;
}
.os-keys-bar {
    display: flex;
    align-items: center;
    background: #090d16;
    border-bottom: 1px solid #1e293b;
    padding: 4px 8px;
    gap: 5px;
    overflow-x: auto;
    white-space: nowrap;
    -webkit-overflow-scrolling: touch;
    scrollbar-width: none;
    flex-shrink: 0;
}
.os-keys-bar::-webkit-scrollbar { display: none; }
.os-key-btn {
    background: #1e293b;
    color: #cbd5e1;
    border: 1px solid #334155;
    border-radius: 6px;
    padding: 0 8px;
    height: 28px;
    font-size: 11px;
    font-weight: 700;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 3px;
    flex-shrink: 0;
    transition: all 0.15s;
}
.os-key-btn:active { transform: scale(0.95); }
.os-key-btn.active {
    background: #2563eb !important;
    color: #ffffff !important;
    border-color: #60a5fa !important;
    box-shadow: 0 0 10px rgba(37, 99, 235, 0.65);
}
.os-key-btn.action {
    background: #1e293b;
    color: #38bdf8;
    border-color: #0284c7;
}
.os-key-btn.action:active { background: #0284c7; color: #fff; }
.screen-kb-drawer {
    display: none;
    flex-direction: column;
    background: #0f172a;
    border-bottom: 1px solid #1e293b;
    padding: 6px 8px;
    gap: 6px;
    flex-shrink: 0;
    animation: slideDown 0.2s ease-out;
}
@keyframes slideDown {
    from { opacity: 0; transform: translateY(-8px); }
    to { opacity: 1; transform: translateY(0); }
}
.kb-input-row {
    display: flex;
    gap: 6px;
}
.kb-input-row input {
    flex: 1;
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 7px;
    padding: 6px 10px;
    color: #fff;
    font-size: 13px;
    outline: none;
    user-select: text;
    -webkit-user-select: text;
}
.kb-input-row input:focus { border-color: #38bdf8; }
.btn-kb-send {
    background: #2563eb;
    color: #fff;
    border: none;
    border-radius: 7px;
    padding: 0 12px;
    font-weight: 700;
    font-size: 12px;
    cursor: pointer;
}
.btn-kb-send:active { transform: scale(0.96); }
.kb-quick-keys {
    display: flex;
    gap: 4px;
    overflow-x: auto;
    white-space: nowrap;
    scrollbar-width: none;
}
.kb-quick-keys::-webkit-scrollbar { display: none; }
.kb-key {
    background: #1e293b;
    color: #cbd5e1;
    border: 1px solid #334155;
    border-radius: 5px;
    padding: 4px 8px;
    font-size: 11px;
    font-weight: 600;
    cursor: pointer;
    flex-shrink: 0;
}
.kb-key:active { background: #3b82f6; color: #fff; }
.touch-ripple {
    position: fixed;
    transform: translate(-50%, -50%);
    width: 44px;
    height: 44px;
    border-radius: 50%;
    background: rgba(56, 189, 248, 0.35);
    border: 2px solid #38bdf8;
    pointer-events: none;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 10px;
    font-weight: 700;
    color: #fff;
    animation: rippleAnim 0.5s ease-out forwards;
    z-index: 9999;
}
@keyframes rippleAnim {
    0% { transform: translate(-50%, -50%) scale(0.6); opacity: 1; }
    100% { transform: translate(-50%, -50%) scale(1.6); opacity: 0; }
}
.screen-bottom-bar {
    display: flex;
    padding: 5px 8px;
    gap: 6px;
    background: #0f172a;
    border-top: 1px solid #1e293b;
    flex-shrink: 0;
}
.btn-screen-click {
    flex: 1;
    height: 38px;
    border-radius: 8px;
    border: 1px solid #334155;
    background: #1e293b;
    color: #f1f5f9;
    font-weight: 600;
    font-size: 12px;
    cursor: pointer;
}
.btn-screen-click:active { background: #2563eb; }

/* TAB 3: SLIDES & LASER */
.slides-container {
    flex: 1 1 0%;
    min-height: 0;
    display: flex;
    flex-direction: column;
    padding: 10px;
    gap: 8px;
    overflow-y: auto;
    -webkit-overflow-scrolling: touch;
}
.slide-status-card {
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 10px;
    padding: 8px 12px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    flex-shrink: 0;
}
.slide-num { font-size: 16px; font-weight: 700; color: #38bdf8; }
.slide-timer { font-size: 13px; font-weight: 600; color: #a78bfa; font-variant-numeric: tabular-nums; }
.notes-card {
    background: #162032;
    border: 1px solid #2d3b55;
    border-radius: 10px;
    padding: 8px 12px;
    max-height: 90px;
    overflow-y: auto;
    font-size: 12px;
    line-height: 1.4;
    color: #cbd5e1;
    user-select: text;
    -webkit-user-select: text;
    flex-shrink: 0;
}
.notes-label { font-size: 10px; font-weight: 700; color: #94a3b8; text-transform: uppercase; margin-bottom: 2px; }
.slide-nav-buttons {
    display: flex;
    gap: 8px;
    height: 75px;
    flex-shrink: 0;
}
.btn-slide-nav {
    flex: 1;
    border-radius: 12px;
    border: none;
    font-size: 15px;
    font-weight: 700;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 3px;
    cursor: pointer;
    box-shadow: 0 3px 8px rgba(0,0,0,0.3);
}
.btn-prev { background: #334155; color: #f8fafc; }
.btn-next { background: #2563eb; color: #fff; }
.btn-slide-nav:active { transform: scale(0.97); }

.laser-pad {
    height: 110px;
    background: radial-gradient(circle at center, #2d1515 0%, #150a0a 100%);
    border: 2px dashed #ef4444;
    border-radius: 12px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    color: #fca5a5;
    font-size: 12px;
    font-weight: 600;
    touch-action: none;
    position: relative;
    box-shadow: 0 0 12px rgba(239, 68, 68, 0.2);
    flex-shrink: 0;
}
.laser-pad:active {
    background: radial-gradient(circle at center, #451010 0%, #1a0808 100%);
    border-style: solid;
    box-shadow: 0 0 20px rgba(239, 68, 68, 0.5);
}

.slide-controls-row {
    display: flex;
    gap: 6px;
    flex-shrink: 0;
}
.btn-ctrl {
    flex: 1;
    padding: 8px 4px;
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 8px;
    color: #cbd5e1;
    font-size: 11.5px;
    font-weight: 600;
    cursor: pointer;
}
.btn-ctrl:active { background: #3b82f6; color: #fff; }

/* TAB 4: SIGNATURE */
.sign-container {
    flex: 1 1 0%;
    min-height: 0;
    display: flex;
    flex-direction: column;
    padding: 8px;
    gap: 8px;
    overflow: hidden;
}
.canvas-wrapper {
    flex: 1 1 0%;
    min-height: 0;
    background: #ffffff;
    border-radius: 10px;
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
.palette { display: flex; gap: 6px; }
.color-dot {
    width: 28px;
    height: 28px;
    border-radius: 50%;
    border: 2px solid transparent;
    cursor: pointer;
}
.color-dot.active { border-color: #fff; transform: scale(1.1); }
.btn-sign-insert {
    background: #10b981;
    color: #fff;
    border: none;
    border-radius: 8px;
    padding: 10px 14px;
    font-size: 13px;
    font-weight: 700;
    cursor: pointer;
    flex: 1;
    box-shadow: 0 2px 8px rgba(16, 185, 129, 0.4);
}
.btn-sign-insert:active { transform: scale(0.97); }

/* TAB 5: NUMPAD */
.numpad-container {
    flex: 1 1 0%;
    min-height: 0;
    display: flex;
    flex-direction: column;
    padding: 8px;
    gap: 6px;
    overflow: hidden;
}
.text-input-box {
    display: flex;
    gap: 6px;
    flex-shrink: 0;
}
.text-input-box input {
    flex: 1;
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 8px;
    padding: 8px 10px;
    color: #fff;
    font-size: 14px;
    outline: none;
    user-select: text;
    -webkit-user-select: text;
}
.text-input-box button {
    background: #2563eb;
    color: #fff;
    border: none;
    border-radius: 8px;
    padding: 0 14px;
    font-weight: 600;
    font-size: 13px;
}
.numpad-grid {
    flex: 1 1 0%;
    min-height: 0;
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    grid-template-rows: repeat(5, 1fr);
    gap: 6px;
}
.npbtn {
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 8px;
    color: #f1f5f9;
    font-size: 18px;
    font-weight: 600;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    box-shadow: 0 2px 4px rgba(0,0,0,0.25);
}
.npbtn:active { background: #3b82f6; transform: scale(0.96); }
.npbtn.accent { background: #334155; color: #38bdf8; font-size: 16px; }
.npbtn.enter { background: #2563eb; color: #fff; font-size: 14px; }

/* Bottom Tab Bar */
.tab-bar {
    background: #111827;
    border-top: 1px solid #1f2937;
    display: flex;
    min-height: 56px;
    height: 56px;
    height: calc(56px + env(safe-area-inset-bottom, 0px));
    flex-shrink: 0;
    padding-bottom: env(safe-area-inset-bottom, 0px);
    z-index: 1000;
}
.tab-item {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 2px;
    color: #94a3b8;
    font-size: 10px;
    font-weight: 600;
    cursor: pointer;
    transition: color 0.2s, background-color 0.2s;
    padding: 4px 2px;
}
.tab-item.active { color: #38bdf8; background: rgba(56, 189, 248, 0.08); border-radius: 8px; }
.tab-icon { font-size: 19px; }
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
    <div class=""qbtn"" style=""background:#2563eb; color:#fff; font-weight:700; border-color:#3b82f6;"" onclick=""switchTab(2)"">🖥️ Xem Màn Hình PC</div>
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
            <span style=""font-size: 26px; margin-bottom: 6px;"">👆</span>
            <span>Rê ngón tay để di chuột</span>
            <span style=""font-size: 11px; margin-top: 3px; color: #475569;"">Chạm 1 ngón = Trái • 2 ngón = Cuộn mượt</span>
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
    <div style=""padding: 0 8px 6px 8px; display: flex; flex-shrink: 0;"">
        <button onclick=""switchTab(2)"" style=""width: 100%; height: 38px; background: linear-gradient(135deg, #1e40af, #2563eb); color: #fff; border: 1px solid #3b82f6; border-radius: 8px; font-size: 12.5px; font-weight: 700; display: flex; align-items: center; justify-content: center; gap: 8px; cursor: pointer; box-shadow: 0 2px 8px rgba(37,99,235,0.35);"">
            🖥️ XEM TRỰC TIẾP MÀN HÌNH PC (LIVE STREAM)
        </button>
    </div>
</div>

<!-- TAB 2: LIVE SCREEN MIRROR -->
<div class=""tab-content"" id=""tab2"">
    <div class=""screen-container"">
        <div class=""screen-toolbar"">
            <div class=""screen-stat"" id=""screenStatBadge"">● LIVE 15 FPS</div>
            <div style=""display:flex; gap:5px;"">
                <button class=""sbtn"" onclick=""switchTab(1)"" style=""background:#334155;"">🖱️ Touchpad</button>
                <button class=""sbtn"" id=""btnToggleStream"" onclick=""toggleScreenStream()"">⏸ Dừng</button>
                <button class=""sbtn"" id=""btnQuality"" onclick=""cycleScreenQuality()"">⚡ Chuẩn</button>
                <button class=""sbtn"" onclick=""toggleFullscreen()"">⛶ Toàn màn hình</button>
            </div>
        </div>

        <!-- Sticky Modifier Keys & Action Bar (Hệ Điều Hành) -->
        <div class=""os-keys-bar"">
            <button class=""os-key-btn"" id=""btnMod_ctrl"" onclick=""toggleStickyKey('ctrl')"">CTRL</button>
            <button class=""os-key-btn"" id=""btnMod_alt"" onclick=""toggleStickyKey('alt')"">ALT</button>
            <button class=""os-key-btn"" id=""btnMod_shift"" onclick=""toggleStickyKey('shift')"">SHIFT</button>
            <button class=""os-key-btn"" id=""btnMod_win"" onclick=""toggleStickyKey('win')"">WIN</button>
            <button class=""os-key-btn action"" onclick=""sendKeyPress('prtscn')"">📸 Chụp Màn Hình</button>
            <button class=""os-key-btn action"" id=""btnToggleKb"" onclick=""toggleScreenKeyboard()"">⌨️ Bàn Phím</button>
            <button class=""os-key-btn"" onclick=""sendKeyPress('esc')"">ESC</button>
            <button class=""os-key-btn"" onclick=""sendKeyPress('tab')"">TAB</button>
            <button class=""os-key-btn"" onclick=""sendKeyPress('del')"">DEL</button>
        </div>

        <!-- Inline Keyboard Drawer -->
        <div class=""screen-kb-drawer"" id=""screenKbDrawer"">
            <div class=""kb-input-row"">
                <input type=""text"" id=""screenTextInput"" placeholder=""Nhập văn bản gửi vào PC..."" autocomplete=""off"">
                <button class=""btn-kb-send"" onclick=""sendScreenTextInput()"">Gửi ↵</button>
            </div>
            <div class=""kb-quick-keys"">
                <button class=""kb-key"" onclick=""sendKeyPress('enter')"">↵ Enter</button>
                <button class=""kb-key"" onclick=""sendKeyPress('backspace')"">⌫ Xóa</button>
                <button class=""kb-key"" onclick=""sendKeyPress('space')"">␣ Space</button>
                <button class=""kb-key"" onclick=""sendKeyPress('f5')"">⛶ F5</button>
                <button class=""kb-key"" onclick=""sendKeyPress('left')"">←</button>
                <button class=""kb-key"" onclick=""sendKeyPress('up')"">↑</button>
                <button class=""kb-key"" onclick=""sendKeyPress('down')"">↓</button>
                <button class=""kb-key"" onclick=""sendKeyPress('right')"">→</button>
            </div>
        </div>

        <!-- Touch Screen Viewport -->
        <div class=""screen-viewport"" id=""screenViewport"">
            <img id=""screenLiveImg"" alt=""Màn hình PC..."" draggable=""false"" />
            <div class=""screen-touch-hint"">👆 1 chạm = Trái • 2 chạm = Đúp • Giữ / 2 ngón = Phải • Kéo đúp = Kéo thả • 2 ngón = Cuộn</div>
        </div>

        <!-- Bottom Quick Actions -->
        <div class=""screen-bottom-bar"">
            <button class=""btn-screen-click"" onclick=""sendScreenClick('left')"">🖱️ Nhấp Trái</button>
            <button class=""btn-screen-click"" onclick=""sendScreenClick('double')"">🖱️ Nhấp Đúp</button>
            <button class=""btn-screen-click"" onclick=""sendScreenClick('right')"">🖱️ Nhấp Phải</button>
            <button class=""btn-screen-click"" onclick=""toggleScreenKeyboard()"" style=""background:#334155;"">⌨️ Bàn Phím</button>
        </div>
    </div>
</div>

<!-- TAB 3: SLIDES & LASER -->
<div class=""tab-content"" id=""tab3"">
    <div class=""slides-container"">
        <div class=""slide-status-card"">
            <div>
                <div style=""font-size: 10px; color: #94a3b8;"">SLIDE HIỆN TẠI</div>
                <div class=""slide-num"" id=""slideNumText"">Slide 1 / 1</div>
            </div>
            <div style=""text-align: right;"">
                <div style=""font-size: 10px; color: #94a3b8;"">THỜI GIAN</div>
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
            <span style=""font-size: 24px; margin-bottom: 3px;"">🔴</span>
            <span>BÚT LASER ẢO</span>
            <span style=""font-size: 10.5px; opacity: 0.8;"">Giữ & rê ngón tay để chiếu tia đỏ lên PC</span>
        </div>

        <div class=""slide-controls-row"">
            <button class=""btn-ctrl"" onclick=""sendSlideAction('black')"">⬛ Màn Đen (B)</button>
            <button class=""btn-ctrl"" onclick=""sendSlideAction('white')"">⬜ Màn Trắng (W)</button>
            <button class=""btn-ctrl"" onclick=""sendSlideAction('stop')"">⏹️ Thoát Chiếu</button>
        </div>
    </div>
</div>

<!-- TAB 4: SIGNATURE & DRAWING -->
<div class=""tab-content"" id=""tab4"">
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

<!-- TAB 5: NUMPAD & INPUT -->
<div class=""tab-content"" id=""tab5"">
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
        <div class=""tab-icon"">🖥️</div>
        <span>Màn Hình</span>
    </div>
    <div class=""tab-item"" onclick=""switchTab(3)"">
        <div class=""tab-icon"">📽️</div>
        <span>Trình Chiếu</span>
    </div>
    <div class=""tab-item"" onclick=""switchTab(4)"">
        <div class=""tab-icon"">✍️</div>
        <span>Chữ Ký</span>
    </div>
    <div class=""tab-item"" onclick=""switchTab(5)"">
        <div class=""tab-icon"">🔢</div>
        <span>Bàn Phím</span>
    </div>
</div>

<script>
let ws = null;
let isConnected = false;
let currentTab = 1;

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
    currentTab = idx;
    document.querySelectorAll('.tab-content').forEach((el, i) => {
        el.classList.toggle('active', i === idx - 1);
    });
    document.querySelectorAll('.tab-item').forEach((el, i) => {
        el.classList.toggle('active', i === idx - 1);
    });
    if (idx === 2) {
        startScreenStream();
    } else {
        stopScreenStream();
    }
    if (idx === 4) initCanvasSize();
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

// ============================================
// TOUCHPAD & CHUỘT CẢM ỨNG (KHẮC PHỤC NHẢY CHUỘT & CUỘN QUÁ NHANH)
// ============================================
const pad = document.getElementById('touchpadPad');
let lastTouchX = 0, lastTouchY = 0;
let lastTouchCount = 0;
let touchStartTime = 0;
let isTouchMoved = false;

pad.addEventListener('touchstart', (e) => {
    lastTouchCount = e.touches.length;
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
    // Nếu số ngón tay thay đổi (ví dụ từ 2 ngón về 1 ngón hoặc ngược lại):
    // Cập nhật lại tọa độ mốc ngay lập tức, không sinh delta để tránh chuột bị văng xa
    if (e.touches.length !== lastTouchCount) {
        lastTouchCount = e.touches.length;
        if (e.touches.length === 1) {
            lastTouchX = e.touches[0].clientX;
            lastTouchY = e.touches[0].clientY;
        } else if (e.touches.length === 2) {
            lastTouchX = (e.touches[0].clientX + e.touches[1].clientX) / 2;
            lastTouchY = (e.touches[0].clientY + e.touches[1].clientY) / 2;
        }
        return;
    }

    if (e.touches.length === 1) {
        const cx = e.touches[0].clientX;
        const cy = e.touches[0].clientY;
        const dx = cx - lastTouchX;
        const dy = cy - lastTouchY;
        lastTouchX = cx;
        lastTouchY = cy;

        // Bỏ qua nhiễu micro-jitter < 0.4px
        if (Math.abs(dx) > 0.4 || Math.abs(dy) > 0.4) {
            isTouchMoved = true;
            send({ type: 'mouse_move', dx: dx, dy: dy, sens: 1.6 });
        }
    } else if (e.touches.length === 2) {
        const cy = (e.touches[0].clientY + e.touches[1].clientY) / 2;
        const dy = cy - lastTouchY;
        lastTouchY = cy;
        // Dịu tốc độ cuộn: dùng hệ số dịu 0.55 và kẹp biên độ tối đa để tránh bay xa ở vùng animation
        let clampedDy = Math.max(-25, Math.min(25, dy * 0.55));
        send({ type: 'mouse_scroll', dx: 0, dy: clampedDy });
    }
}, { passive: false });

pad.addEventListener('touchend', (e) => {
    if (e.touches.length === 0) {
        const duration = Date.now() - touchStartTime;
        if (!isTouchMoved && duration < 250) {
            sendClick('left');
        }
        lastTouchCount = 0;
    } else if (e.touches.length === 1) {
        // Vẫn còn 1 ngón trên màn hình: reset mốc tọa độ ngay về ngón này
        lastTouchX = e.touches[0].clientX;
        lastTouchY = e.touches[0].clientY;
        lastTouchCount = 1;
        isTouchMoved = true;
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
    // Giảm tốc độ và kẹp biên độ tối đa
    let clampedDy = Math.max(-28, Math.min(28, dy * 0.7));
    send({ type: 'mouse_scroll', dx: 0, dy: clampedDy });
}, { passive: false });

// ============================================
// CHIA SẺ MÀN HÌNH PC TRỰC TIẾP (SCREEN MIRRORING)
// ============================================
let screenStreaming = false;
let screenQualityMode = 'normal'; // normal, fast, hd
let screenFps = 15;
let screenRafId = null;
const screenImg = document.getElementById('screenLiveImg');

function toggleScreenStream() {
    vibe(20);
    const btn = document.getElementById('btnToggleStream');
    if (screenStreaming) {
        stopScreenStream();
        btn.textContent = '▶ Tiếp tục';
        document.getElementById('screenStatBadge').textContent = '⏸ Tạm dừng';
        document.getElementById('screenStatBadge').style.color = '#f59e0b';
    } else {
        startScreenStream();
        btn.textContent = '⏸ Dừng';
        document.getElementById('screenStatBadge').textContent = '● LIVE 15 FPS';
        document.getElementById('screenStatBadge').style.color = '#10b981';
    }
}

function cycleScreenQuality() {
    vibe(20);
    const btn = document.getElementById('btnQuality');
    if (screenQualityMode === 'normal') {
        screenQualityMode = 'hd';
        btn.textContent = '🌟 HD 720p';
        screenFps = 12;
    } else if (screenQualityMode === 'hd') {
        screenQualityMode = 'fast';
        btn.textContent = '⚡ Siêu nhanh';
        screenFps = 20;
    } else {
        screenQualityMode = 'normal';
        btn.textContent = '⚡ Chuẩn';
        screenFps = 15;
    }
}

function toggleFullscreen() {
    vibe(20);
    if (!document.fullscreenElement) {
        document.documentElement.requestFullscreen().catch(()=>{});
    } else {
        if (document.exitFullscreen) document.exitFullscreen();
    }
}

function startScreenStream() {
    if (screenStreaming) return;
    screenStreaming = true;
    requestNextScreenFrame();
}

function stopScreenStream() {
    screenStreaming = false;
    if (screenRafId) {
        clearTimeout(screenRafId);
        screenRafId = null;
    }
}

function requestNextScreenFrame() {
    if (!screenStreaming || currentTab !== 2) return;
    const nextImg = new Image();
    const t = Date.now();
    let qParam = (screenQualityMode === 'hd') ? 'hd' : ((screenQualityMode === 'fast') ? 'fast' : 'normal');

    nextImg.onload = function() {
        if (screenStreaming && currentTab === 2) {
            screenImg.src = nextImg.src;
            screenRafId = setTimeout(requestNextScreenFrame, 1000 / screenFps);
        }
    };
    nextImg.onerror = function() {
        if (screenStreaming && currentTab === 2) {
            screenRafId = setTimeout(requestNextScreenFrame, 500);
        }
    };
    nextImg.src = '/screen.jpg?q=' + qParam + '&t=' + t;
}

// ============================================
// HỆ ĐIỀU HÀNH THU NHỎ & PHÍM TẮT DÍNH (STICKY KEYS)
// ============================================
const stickyKeys = { ctrl: false, alt: false, shift: false, win: false };

function toggleStickyKey(key) {
    vibe(25);
    stickyKeys[key] = !stickyKeys[key];
    const btn = document.getElementById('btnMod_' + key);
    if (stickyKeys[key]) {
        if (btn) btn.classList.add('active');
        send({ type: 'key_down', key: key });
    } else {
        if (btn) btn.classList.remove('active');
        send({ type: 'key_up', key: key });
    }
}

function sendKeyPress(key) {
    vibe(20);
    send({ type: 'key_press', key: key });
}

function toggleScreenKeyboard() {
    vibe(20);
    const drawer = document.getElementById('screenKbDrawer');
    const btn = document.getElementById('btnToggleKb');
    if (!drawer) return;
    if (drawer.style.display === 'none' || !drawer.style.display) {
        drawer.style.display = 'flex';
        if (btn) btn.classList.add('active');
        const inp = document.getElementById('screenTextInput');
        if (inp) inp.focus();
    } else {
        drawer.style.display = 'none';
        if (btn) btn.classList.remove('active');
    }
}

function sendScreenTextInput() {
    const inp = document.getElementById('screenTextInput');
    if (inp && inp.value) {
        vibe(25);
        send({ type: 'type_text', text: inp.value });
        inp.value = '';
    }
}

// ============================================
// TEAMVIEWER TOUCH GESTURE ENGINE CHO MÀN HÌNH PC
// ============================================
const sViewport = document.getElementById('screenViewport');
let stCount = 0;
let stStartX = 0, stStartY = 0;
let stStartTime = 0;
let stLastX = 0, stLastY = 0;
let stMoved = false;
let stLongPressTimer = null;
let stIsLongPressed = false;
let stLastTapTime = 0;
let stLastTapX = 0, stLastTapY = 0;
let stSingleTapTimer = null;
let stIsDoubleTapDragging = false;
let stTwoFingerStartY = 0;
let stTwoFingerStartTime = 0;
let stTwoFingerMoved = false;

function getScreenNorm(clientX, clientY) {
    const rect = screenImg.getBoundingClientRect();
    if (rect.width <= 0 || rect.height <= 0) return { x: 0.5, y: 0.5 };
    const nx = Math.max(0, Math.min(1, (clientX - rect.left) / rect.width));
    const ny = Math.max(0, Math.min(1, (clientY - rect.top) / rect.height));
    return { x: nx, y: ny };
}

function showTouchFeedback(clientX, clientY, text) {
    const fb = document.createElement('div');
    fb.className = 'touch-ripple';
    fb.textContent = text || '';
    fb.style.left = clientX + 'px';
    fb.style.top = clientY + 'px';
    document.body.appendChild(fb);
    setTimeout(() => { if (fb.parentNode) fb.parentNode.removeChild(fb); }, 550);
}

sViewport.addEventListener('touchstart', (e) => {
    e.preventDefault();
    stCount = e.touches.length;

    if (e.touches.length === 1) {
        const t = e.touches[0];
        stStartX = t.clientX;
        stStartY = t.clientY;
        stLastX = t.clientX;
        stLastY = t.clientY;
        stStartTime = Date.now();
        stMoved = false;
        stIsLongPressed = false;

        // Kiểm tra Nhấn đúp rồi kéo (Double-Tap & Drag - Kéo cửa sổ, bôi đen văn bản)
        const timeSinceLastTap = Date.now() - stLastTapTime;
        const distFromLastTap = Math.hypot(stStartX - stLastTapX, stStartY - stLastTapY);
        if (timeSinceLastTap < 350 && distFromLastTap < 30) {
            if (stSingleTapTimer) { clearTimeout(stSingleTapTimer); stSingleTapTimer = null; }
            stIsDoubleTapDragging = true;
            vibe(30);
            showTouchFeedback(stStartX, stStartY, '✊ Kéo');
            const norm = getScreenNorm(stStartX, stStartY);
            send({ type: 'screen_mouse_down', x: norm.x, y: norm.y, button: 'left' });
            return;
        }

        stIsDoubleTapDragging = false;

        // Đếm nhấn giữ 450ms -> Chuột Phải (Long press Right click)
        if (stLongPressTimer) clearTimeout(stLongPressTimer);
        stLongPressTimer = setTimeout(() => {
            if (!stMoved && stCount === 1) {
                stIsLongPressed = true;
                vibe(45);
                showTouchFeedback(stStartX, stStartY, '🖱️ Phải');
                const norm = getScreenNorm(stStartX, stStartY);
                send({ type: 'screen_click', x: norm.x, y: norm.y, button: 'right' });
            }
        }, 450);

    } else if (e.touches.length === 2) {
        if (stLongPressTimer) { clearTimeout(stLongPressTimer); stLongPressTimer = null; }
        if (stSingleTapTimer) { clearTimeout(stSingleTapTimer); stSingleTapTimer = null; }
        stIsDoubleTapDragging = false;
        stTwoFingerStartY = (e.touches[0].clientY + e.touches[1].clientY) / 2;
        stTwoFingerStartTime = Date.now();
        stTwoFingerMoved = false;
    }
}, { passive: false });

sViewport.addEventListener('touchmove', (e) => {
    e.preventDefault();
    stCount = e.touches.length;

    if (e.touches.length === 1) {
        const t = e.touches[0];
        const dist = Math.hypot(t.clientX - stStartX, t.clientY - stStartY);
        if (dist > 8) {
            stMoved = true;
            if (stLongPressTimer) { clearTimeout(stLongPressTimer); stLongPressTimer = null; }
        }

        stLastX = t.clientX;
        stLastY = t.clientY;
        const norm = getScreenNorm(t.clientX, t.clientY);

        if (stIsDoubleTapDragging) {
            send({ type: 'screen_move', x: norm.x, y: norm.y });
        } else if (dist > 6) {
            send({ type: 'screen_move', x: norm.x, y: norm.y });
        }

    } else if (e.touches.length === 2) {
        if (stLongPressTimer) { clearTimeout(stLongPressTimer); stLongPressTimer = null; }
        const cy = (e.touches[0].clientY + e.touches[1].clientY) / 2;
        const dy = cy - stTwoFingerStartY;
        stTwoFingerStartY = cy;

        if (Math.abs(dy) > 1.5) {
            stTwoFingerMoved = true;
            // Cuộn 2 ngón tay dịu mượt, tránh giật trôi ở vùng animation
            let clampedDy = Math.max(-28, Math.min(28, dy * 0.65));
            send({ type: 'screen_scroll', dy: clampedDy });
        }
    }
}, { passive: false });

sViewport.addEventListener('touchend', (e) => {
    if (stLongPressTimer) { clearTimeout(stLongPressTimer); stLongPressTimer = null; }

    if (stIsDoubleTapDragging) {
        stIsDoubleTapDragging = false;
        const norm = getScreenNorm(stLastX, stLastY);
        send({ type: 'screen_mouse_up', x: norm.x, y: norm.y, button: 'left' });
        stLastTapTime = 0;
        return;
    }

    if (e.touches.length === 0) {
        if (stIsLongPressed) {
            stLastTapTime = 0;
            return;
        }

        // Chạm 2 ngón tay nhanh (< 280ms, không cuộn) = Chuột Phải
        if (stCount === 2 && !stTwoFingerMoved && (Date.now() - stTwoFingerStartTime < 280)) {
            vibe(35);
            showTouchFeedback(stLastX, stLastY, '🖱️ Phải');
            const norm = getScreenNorm(stLastX, stLastY);
            send({ type: 'screen_click', x: norm.x, y: norm.y, button: 'right' });
            stLastTapTime = 0;
            return;
        }

        // Chạm 1 ngón tay ngắn không di chuyển
        if (!stMoved && (Date.now() - stStartTime < 280)) {
            const timeSinceLastTap = Date.now() - stLastTapTime;
            const distFromLastTap = Math.hypot(stStartX - stLastTapX, stStartY - stLastTapY);

            if (timeSinceLastTap < 300 && distFromLastTap < 25) {
                // Nhấn đúp (Double Tap) = Double Click
                if (stSingleTapTimer) { clearTimeout(stSingleTapTimer); stSingleTapTimer = null; }
                vibe(35);
                showTouchFeedback(stStartX, stStartY, '⚡ Nhấp đúp');
                const norm = getScreenNorm(stStartX, stStartY);
                send({ type: 'screen_click', x: norm.x, y: norm.y, button: 'double' });
                stLastTapTime = 0;
            } else {
                stLastTapTime = Date.now();
                stLastTapX = stStartX;
                stLastTapY = stStartY;
                const norm = getScreenNorm(stStartX, stStartY);
                stSingleTapTimer = setTimeout(() => {
                    vibe(20);
                    showTouchFeedback(stLastTapX, stLastTapY, '');
                    send({ type: 'screen_click', x: norm.x, y: norm.y, button: 'left' });
                    stSingleTapTimer = null;
                }, 190);
            }
        }
    }
});

function sendScreenClick(button) {
    vibe(25);
    send({ type: 'mouse_click', button: button });
}

// ============================================
// BÚT LASER ẢO & TRÌNH CHIẾU
// ============================================
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

// ============================================
// CHỮ KÝ & VẼ TAY CANVAS
// ============================================
const canvas = document.getElementById('signCanvas');
const ctx = canvas.getContext('2d');
let drawing = false;
let penColor = '#0f4c81';

function initCanvasSize() {
    const wrapper = document.getElementById('canvasWrapper');
    if (!wrapper) return;
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
