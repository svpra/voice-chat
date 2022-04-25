using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using NAudio.Wave;
using NAudio.CoreAudioApi;
 
namespace VoiceChat
{
    public partial class Form1 : Form
    {
        //Подключены ли мы
        private bool connected;
        //сокет отправитель
        Socket client;
        //поток для нашей речи
        WaveIn input;
        //поток для речи собеседника
        WaveOut output;
        //буфферный поток для передачи через сеть
        BufferedWaveProvider bufferStream;
        //поток для прослушивания входящих сообщений
        Thread in_thread;
        //сокет для приема (протокол UDP)
        Socket listeningSocket;
 
        public Form1()
        {
            InitializeComponent();
            //создаем поток для записи нашей речи
            input = new WaveIn();
            //определяем его формат - частота дискретизации 8000 Гц, ширина сэмпла - 16 бит, 1 канал - моно
            input.WaveFormat = new WaveFormat(8000, 16, 1);
            //добавляем код обработки нашего голоса, поступающего на микрофон
            input.DataAvailable += Voice_Input;
            //создаем поток для прослушивания входящего звука
            output = new WaveOut();
            //создаем поток для буферного потока и определяем у него такой же формат как и потока с микрофона
            bufferStream = new BufferedWaveProvider(new WaveFormat(8000, 16, 1));
            //привязываем поток входящего звука к буферному потоку
            output.Init(bufferStream);
            //сокет для отправки звука
            client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            connected = true;
            listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //создаем поток для прослушивания
            in_thread = new Thread(new ThreadStart(Listening));
            //запускаем его
            in_thread.Start();
        }
        //Обработка нашего голоса
        private void Voice_Input(object sender, WaveInEventArgs e)
        {
            try
            {
                //Подключаемся к удаленному адресу
                IPEndPoint remote_point = new IPEndPoint(IPAddress.Parse(textBox1.Text), 5555);
                //посылаем байты, полученные с микрофона на удаленный адрес
                client.SendTo(e.Buffer, remote_point);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        //Прослушивание входящих подключений
        private void Listening()
        {
            //Прослушиваем по адресу
            IPEndPoint localIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5555);
            listeningSocket.Bind(localIP);
            //начинаем воспроизводить входящий звук
            output.Play();
            //адрес, с которого пришли данные
            EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
            //бесконечный цикл
            while(connected == true)
            {
                try
                {
                    //промежуточный буфер
                    byte[] data = new byte[65535];
                    //получено данных
                    int received = listeningSocket.ReceiveFrom(data, ref remoteIp);
                    //добавляем данные в буфер, откуда output будет воспроизводить звук
                    bufferStream.AddSamples(data, 0, received);
                }
                catch (SocketException ex)
                {}       
            }
        }
 
        private void button1_Click(object sender, EventArgs e)
        {
            input.StartRecording();
        }
 
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            connected = false;
            listeningSocket.Close();
            listeningSocket.Dispose();
 
            client.Close();
            client.Dispose();
            if (output != null)
            {
                output.Stop();
                output.Dispose();
                output = null;
            }
            if (input != null)
            {
                input.Dispose();
                input = null;
            }
            bufferStream = null;   
         }
    }
}
