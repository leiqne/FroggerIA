using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
[System.Serializable]



public abstract class Personaje : MonoBehaviour
{
    public struct Entorno
    {
        public int[,] datos;

        public string convertToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("[\n");

            for (int i = 0; i < datos.GetLength(0); i++)
            {
                stringBuilder.Append("  [");

                for (int j = 0; j < datos.GetLength(1); j++)
                {
                    stringBuilder.Append(datos[i, j]);

                    if (j < datos.GetLength(1) - 1)
                    {
                        stringBuilder.Append(", ");
                    }
                }

                stringBuilder.Append("]");

                if (i < datos.GetLength(0) - 1)
                {
                    stringBuilder.Append(",\n");
                }
                else
                {
                    stringBuilder.Append("\n");
                }
            }

            stringBuilder.Append("]");
            return stringBuilder.ToString();
        }


    }
    public static Personaje instance;
    public static int dataBufferSize = 1234;
    private string ip = "0.tcp.sa.ngrok.io";
    private int port = 11554;
    public TCP tcp;


    private SpriteRenderer sprite1;
    public Sprite sSalto;
    public Sprite sDfault;
    public Sprite sMuerto;
    private Vector3 spawnpos;
    protected abstract bool MoveUpInput();
    protected abstract bool MoveDownInput();
    protected abstract bool MoveLeftInput();
    protected abstract bool MoveRightInput();
    private void Awake()
    {
        sprite1 = GetComponent<SpriteRenderer>();
        spawnpos = transform.position;
        if (instance == null)
        {
            instance = this;
        }

    }
    private void Update()
    {
        // Mover hacia arriba
        if (MoveUpInput())
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            MoveCharacter(Vector3.up);
        }
        // Mover hacia abajo
        else if (MoveDownInput())
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            MoveCharacter(Vector3.down);
        }
        // Mover hacia la izquierda
        else if (MoveLeftInput())
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            MoveCharacter(Vector3.left);
        }
        // Mover hacia la derecha
        else if (MoveRightInput())
        {
            transform.rotation = Quaternion.Euler(0f, 0f, -90f);
            MoveCharacter(Vector3.right);
        }





    }
    public void MoveCharacter(Vector3 direc)
    {
        Vector3 dest = transform.position + direc;
        Collider2D barrera = Physics2D.OverlapBox(dest, Vector2.zero, 0f, LayerMask.GetMask("barrera"));
        Collider2D plataforma = Physics2D.OverlapBox(dest, Vector2.zero, 0f, LayerMask.GetMask("plataforma"));
        Collider2D obs = Physics2D.OverlapBox(dest, Vector2.zero, 0f, LayerMask.GetMask("obstaculo"));

        if (barrera != null) { return; }

        if (plataforma != null)
        {
            transform.SetParent(plataforma.transform);
        }
        else
        {
            transform.SetParent(null);
        }

        if (obs != null && plataforma == null)
        {
            transform.position = dest;
            Muerto();
        }
        else
        {
            StartCoroutine(Salto(dest));
        }
    }
    private IEnumerator Salto(Vector3 desti)
    {
        Vector3 stPosition = transform.position;
        float trans = 0f;
        float duracion = 0.1f;
        sprite1.sprite = sSalto;

        while (trans < duracion)
        {
            float t = trans / duracion;
            transform.position = Vector3.Lerp(stPosition, desti, t);
            trans += Time.deltaTime;
            yield return null;
        }

        transform.position = desti;
        sprite1.sprite = sDfault;
    }
    public void respawn()
    {
        StopAllCoroutines();
        transform.rotation = Quaternion.identity;
        transform.position = spawnpos;
        sprite1.sprite = sDfault;
        gameObject.SetActive(true);
        enabled = true;
    }
    protected void Muerto()
    {
        StopAllCoroutines();
        transform.rotation = Quaternion.identity;
        sprite1.sprite = sMuerto;
        enabled = false;

        Invoke(nameof(respawn), 1f);//para que le de un time out cuando muera
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enabled && other.gameObject.layer == LayerMask.NameToLayer("obstaculo") && transform.parent == null)
        {
            Muerto(); /*sprite1.sprite = sMuerto;*/
        }
    }
    public Entorno ObtenerEntorno(int rango)
    {
        Vector3 posicionActual = transform.position;
        Entorno entorno = new Entorno();
        entorno.datos = new int[rango * 2 + 1, rango * 2 + 1];

        for (int i = -rango; i <= rango; i++)
        {
            for (int j = -rango; j <= rango; j++)
            {
                Vector3 posicionAnalizada = posicionActual + new Vector3(i, j, 0);

                // Verificar si hay un obst�culo en la posici�n analizada (usar tu l�gica espec�fica)
                Collider2D obs = Physics2D.OverlapBox(posicionAnalizada, Vector2.zero, 0f, LayerMask.GetMask("obstaculo"));

                if (obs != null)
                {
                    // Hay un obst�culo en esta posici�n
                    entorno.datos[i + rango, j + rango] = 4;
                }
                else if (posicionAnalizada == spawnpos)
                {
                    // Es la posici�n inicial de la rana
                    entorno.datos[i + rango, j + rango] = 1;
                }
                else
                {
                    // Es un camino normal (default)
                    entorno.datos[i + rango, j + rango] = 3;
                }
            }
        }

        return entorno;
    }
    private void Start()
    {
        tcp = new TCP();

    }

    public Entorno ObtenerEntornoDelPersonaje(int rango)
    {
        return ObtenerEntorno(rango);
    }
    public void connectToServer()
    {
        if (tcp.Connect(ip, port))
        {
            UnityEngine.Debug.Log("Conexi�n exitosa al servidor.");

        }
        else
        {
            UnityEngine.Debug.LogError("No se pudo conectar al servidor.");
        }
    }
    public void Disconnect()
    {
        tcp.Close();
    }

    public class TCP
    {
        public TcpClient socket;
        private NetworkStream stream;
        private byte[] receiveBuffer;

        public bool Connect(string ip, int port)
        {
            socket = new TcpClient();
            socket.ReceiveBufferSize = dataBufferSize;
            socket.SendBufferSize = dataBufferSize;

            try
            {
                socket.Connect(ip, port);

                if (!socket.Connected)
                {
                    return false;
                }

                stream = socket.GetStream();
                receiveBuffer = new byte[dataBufferSize];
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error de conexi�n: " + e.Message);
                return false;
            }
        }
        public void Close()
        {

        }

        public void SendData(string data)
        {
            if (socket == null || !socket.Connected)
            {
                UnityEngine.Debug.LogError("No se puede enviar datos, el socket no est� conectado.");
                return;
            }

            try
            {
                byte[] datasend = Encoding.Default.GetBytes(data);
                int length = data.Length;
                byte[] lengthBytes = BitConverter.GetBytes(length);

                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(lengthBytes);
                }

                byte[] headerBytes = new byte[18];

                // Copia la longitud (en formato little-endian) en el encabezado
                Array.Copy(lengthBytes, 0, headerBytes, 0, 4);

                // Agrega el texto "|bot" al encabezado
                byte[] botBytes = Encoding.Default.GetBytes("|bot");
                Array.Copy(botBytes, 0, headerBytes, 4, botBytes.Length);

                // Llena el resto del encabezado con '\x00' si es necesario
                for (int i = 4 + botBytes.Length; i < headerBytes.Length; i++)
                {
                    headerBytes[i] = (byte)'\x00';
                }

                stream.Write(headerBytes, 0, headerBytes.Length);

                int bytesSent = 0;
                int chunkSize = 254; // Tama�o del fragmento

                // Enviar los datos en fragmentos
                while (bytesSent < datasend.Length)
                {
                    int remainingBytes = datasend.Length - bytesSent;
                    int currentChunkSize = Math.Min(chunkSize, remainingBytes);
                    stream.Write(datasend, 0, datasend.Length);
                    bytesSent += currentChunkSize;
                }

            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error al enviar datos: " + e.Message);
            }
        }


        public string ReceiveData()
        {
            try
            {
                // Lee la longitud del mensaje
                byte[] lengthBytes = new byte[4];
                stream.Read(lengthBytes, 0, 4);
                int length = BitConverter.ToInt32(lengthBytes, 0);

                // Lee el encabezado
                byte[] headerBytes = new byte[14];
                stream.Read(headerBytes, 0, 14);
                string header = Encoding.Default.GetString(headerBytes);


                // Lee los datos en fragmentos
                byte[] dataBuffer = new byte[length];
                int totalBytesReceived = 0;

                while (totalBytesReceived < length)
                {
                    int bytesRead = stream.Read(dataBuffer, totalBytesReceived, length - totalBytesReceived);
                    if (bytesRead == 0)
                    {
                        UnityEngine.Debug.Log("La conexi�n se cerr� antes de recibir todos los datos.");
                        break;
                    }
                    totalBytesReceived += bytesRead;
                }

                string receivedMessage = Encoding.Default.GetString(dataBuffer);
                UnityEngine.Debug.Log("Mensaje recibido: " + receivedMessage);

                return receivedMessage;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error al recibir datos: " + e.Message);
                return null;
            }
        }
    };
}