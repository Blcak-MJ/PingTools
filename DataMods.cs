using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PingTools
{
    public class DataMods : INotifyPropertyChanged
    {
        //public string PingContent { get; set; }
        //public bool IsConnected { get; set; }
        //public int Sends { get; set; }
        //public int Receives { get; set; }
        //public int Losses { get; set; }
        //public float LossRate { get; set; }

        //最大时间
        private ulong _maxtime;
        public ulong MaxTime
        {
            get => _maxtime;
            set
            {
                _maxtime = value;
                OnPropertyChanged(nameof(MaxTime));
            }
        }

        //最小时间
        private ulong _mintime;
        public ulong Mintime
        {
            get => _mintime;
            set
            {
                _mintime = value;
                OnPropertyChanged(nameof(Mintime));
            }
        }

        //平均时间
        private float _averagetime;
        public float AverageTime
        {
            get => _averagetime;
            set
            {
                _averagetime = value;
                OnPropertyChanged(nameof(AverageTime));
            }
        }

        //总时间
        private ulong _totaltime;
        public ulong TotalTime
        {
            get => _totaltime;
            set
            {
                _totaltime = value;
                OnPropertyChanged(nameof(TotalTime));
            }
        }

        //已发送计数
        private ulong _sends;
        public ulong Sends
        {
            get => _sends;
            set
            {
                _sends = value;
                OnPropertyChanged(nameof(Sends));
            }
        }


        //已接收计数
        private ulong _receives;
        public ulong Receives
        {
            get => _receives;
            set
            {
                _receives = value;
                OnPropertyChanged(nameof(Receives));
            }
        }

        //丢包计数
        private ulong _losses;
        public ulong Losses
        {
            get => _losses;
            set
            {
                _losses = value;
                OnPropertyChanged(nameof(Losses));
            }
        }

        //丢包率(百分比)
        private double _lossRate;
        public double LossRate
        {
            get => _lossRate;
            set
            {
                _lossRate = value;
                OnPropertyChanged(nameof(LossRate));
            }
        }

        //连接显示
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
            }
        }

        //Ping内容
        private string _PingContent;
        public string PingContent
        {
            get => _PingContent;
            set
            {
                _PingContent = value;
                OnPropertyChanged(nameof(PingContent));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
