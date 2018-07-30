﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Frame;

namespace BeardedManStudios.Source.Forge.Networking
{
    public abstract class  ETCPClientBase : EBaseTCP, IClient
    {
#if WINDOWS_UWP
		private StreamSocket client
#else
        private TcpClient client;
#endif
        // save the hash for upgrade validation
        private string headerHash;
        private BufferManager bufferManager;
        public event BaseNetworkEvent ConnectAttemptFailed;
        public ETCPClientBase(int maxConnections) : base(maxConnections)
        {
            if (maxConnections > 128)
                bufferManager = new BufferManager(8192, 128, (maxConnections / 128) + 1);
            else
                bufferManager = new BufferManager(8192, maxConnections, 2);
        }

        /// <summary>
		/// The identity of the server as a networking player
		/// </summary>
        protected NetworkingPlayer server = null;
        public NetworkingPlayer Server { get { return server; } }

        public virtual void Connect(string host, ushort port = DEFAULT_PORT)
        {
            try
            {
                client = new TcpClient(host, port); // constructor runs connect
            }
            catch (Exception e)
            {
                if(ConnectAttemptFailed != null)
                {
                    ConnectAttemptFailed(this);
                }
            }
            // If we got this far then the bind was successful
            OnBindSuccessful();
            InitializeConnection(host, port);
        }
        protected virtual void InitializeConnection(string host, ushort port)
        {
            // Get a random hash key that needs to be used for validating that the server was connected to
            headerHash = Websockets.HeaderHashKey();

            // This is a typical Websockets accept header to be validated
            byte[] connectionHeader = Websockets.ConnectionHeader(headerHash, port);

            // Register the server as a NetworkingPlayer
            server = new NetworkingPlayer(0, host, true, client, this);
            // Send the upgrade request to the server
            RawWrite(connectionHeader);

            ArraySegment<byte> segment;
            if(!bufferManager.TryTakeBuffer(out segment))  {
                // should probably do something more here //eriknielsen
                throw new OutOfMemoryException("Buffer manager has run out of allocated memory (possible memory leak).");
            }

            ReceiveToken token = new ReceiveToken
            {
                internalBuffer = segment,
                player = server,
                bytesReceived = 0,
                dataHolder = null,
                maxAllowedBytes = 8192
            };

            // Read from the server async


            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveAsync_Completed);
            e.UserToken = token;
            e.BufferList = new List<ArraySegment<byte>> { token.internalBuffer };

            client.Client.ReceiveAsync(e);
        }
        // Should validate the handshake response from the server
        private void ReceiveAsync_Completed(object sender, SocketAsyncEventArgs e)
        {
            
        }
        private void RawWrite(byte[] data)
        {
#if WINDOWS_UWP
			//Write data to the echo server.
			Stream streamOut = client.OutputStream.AsStreamForWrite();
			StreamWriter writer = new StreamWriter(streamOut);
			writer.Write(data);
			writer.Flush();
#else
            client.GetStream().Write(data, 0, data.Length);
#endif
        }
        public override void Disconnect(bool forced)
        {
            throw new NotImplementedException();
        }

        public override void FireRead(FrameStream frame, NetworkingPlayer currentPlayer)
        {
            throw new NotImplementedException();
        }

        public override void Ping()
        {
            throw new NotImplementedException();
        }

        protected override void Pong(NetworkingPlayer playerRequesting, DateTime time)
        {
            throw new NotImplementedException();
        }
    }
}
