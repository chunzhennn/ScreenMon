using Microsoft.EntityFrameworkCore;
using ScreenMonAPI;
using ScreenMonAPI.Messages;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace ScreenMonServer
{
    internal class Server(int port)
    {
        private readonly AppServer _server = new(port);
        private bool _started = false;
        public readonly ConcurrentDictionary<Guid, AppClient> Clients = new();
        private readonly ConcurrentDictionary<int, Session> _sessions = new();
        public readonly ImageManager ImageManager = new(Path.Combine(Directory.GetCurrentDirectory(), "Images"));

        public delegate void ImageEventHandler(object sender, byte[] imageDataBytes, Session session);
        public event ImageEventHandler? ImageReceived;
        public delegate void ClientLoggedInEventHandler(object sender, User user);
        public event ClientLoggedInEventHandler? ClientLoggedIn;
        public delegate void ClientDisconnectedEventHandler(object sender, User user);
        public event ClientDisconnectedEventHandler? ClientDisconnected;
        public delegate void NewUserEventHandler(object sender, User user);
        public event NewUserEventHandler? NewUser;

        public struct ConnectionInfo
        {
            public Guid SessionId;
            public AppClient Client;
        }

        public void Start()
        {
            if (_started)
                throw new InvalidOperationException("Server already started");
            _started = true;
            new Thread(Run).Start();
        }
        public void Run()
        {
            while (true)
            {
                var client = _server.AcceptClient();
                var sessionId = Guid.NewGuid();
                Clients.TryAdd(sessionId, client);
                new Thread(HandleConnection).Start(new ConnectionInfo { Client = client, SessionId = sessionId });
            }
        }

        public void HandleConnection(object? connectionInfo)
        {
            var args = (ConnectionInfo)(connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo)));
            var client = args.Client;
            var sessionId = args.SessionId;
            var loggedIn = false;
            var context = new AppDbContext();
            Session? session = null;
            User? user = null;
            try
            {
                while (!loggedIn)
                {
                    var packet = client.RecvPacket();
                    switch (packet.Type)
                    {
                        case AppPacketType.Image:
                            client.SendMessage(new ResponseMessage
                            { Success = false, Message = "You need to login first" });
                            break;

                        case AppPacketType.Auth:
                            var authMessage = (AuthMessage)packet.Message;
                            user = context.Users.FirstOrDefault(u =>
                                u.Name == authMessage.Username && u.Password == authMessage.Password);
                            if (user == null)
                            {
                                client.SendMessage(new ResponseMessage
                                    { Success = false, Message = "Invalid credential" });
                                break;
                            }
                            
                            session = new Session
                            {
                                Ip = client.Ip,
                                Mac = authMessage.ClientMac,
                                UserId = user.Id,
                                LoginTime = DateTime.Now,
                                Id = sessionId,
                            };
                            if (!_sessions.TryAdd(user.Id, session))
                            {
                                client.SendMessage(new ResponseMessage
                                    { Success = false, Message = "User already logged in" });
                                break;
                            }
                            user.CurrentSession = session;
                            user.LastLoginTime = session.LoginTime;
                            ClientLoggedIn?.Invoke(this, user);
                            Validator.ValidateObject(session, new ValidationContext(session), true);
                            context.Sessions.Add(session);
                            context.Users.First(user => user.Name == authMessage.Username).LastLoginTime =
                                DateTime.Now;
                            context.SaveChanges();
                            loggedIn = true;
                            client.SendMessage(new ResponseMessage { Success = true, Message = "Login successful" });
                            break;
                        case AppPacketType.Register:
                            var registerMessage = (RegisterMessage)packet.Message;
                            var newUser = new User
                                { Name = registerMessage.Username, Password = registerMessage.Password };
                            var validationResults = new List<ValidationResult>();
                            var isValid = Validator.TryValidateObject(newUser, new ValidationContext(newUser),
                                validationResults, true);
                            if (!isValid)
                            {
                                client.SendMessage(new ResponseMessage
                                {
                                    Success = false,
                                    Message = string.Join(';', validationResults.Select(r => r.ErrorMessage))
                                });
                                break;
                            }
                            try
                            {
                                context.Users.Add(newUser);
                                context.SaveChanges();
                                NewUser?.Invoke(this, newUser);
                                client.SendMessage(new ResponseMessage
                                {
                                    Success = true,
                                    Message = "Register succcess"
                                });
                            }
                            catch (DbUpdateException)
                            {
                                client.SendMessage(new ResponseMessage
                                {
                                    Success = false,
                                    Message = "Internal server error"
                                });
                            }

                            break;
                        case AppPacketType.Response:
                        case AppPacketType.FrequencyChange:
                        default:
                            throw new InvalidPacketException("Invalid PacketType");
                    }
                }

                while (true)
                {
                    var packet = client.RecvPacket();
                    switch (packet.Type)
                    {
                        case AppPacketType.Image:
                            var imageMessage = (ImageMessage)packet.Message;
                            Debug.Assert(session != null, "session is null");
                            using (var stream = new MemoryStream())
                            {
                                ImageManager.SaveImage(session.Id, imageMessage.ImageData);
                                ImageReceived?.Invoke(this, imageMessage.ImageData, session);
                            }

                            break;

                        case AppPacketType.Register:
                        case AppPacketType.Auth:
                            client.SendMessage(new ResponseMessage
                            { Success = false, Message = "You've already logged in" });
                            break;


                        case AppPacketType.Response:
                        case AppPacketType.FrequencyChange:
                        default:
                            throw new InvalidPacketException("Invalid PacketType");
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                Clients.Remove(sessionId, out _);
                if (loggedIn)
                {
                    Debug.Assert(user != null, "user is null");
                    _sessions.TryRemove(user.Id, out _);
                    ClientDisconnected?.Invoke(this, user);
                }
                client.Dispose();
            }
        }
    }
}
