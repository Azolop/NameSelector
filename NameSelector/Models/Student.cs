using System.ComponentModel;

namespace NameSelector.Models
{
    /// <summary>
    /// 一名学生。IsCalled/Order 的变化会通知界面即时刷新卡片样式。
    /// </summary>
    public class Student : INotifyPropertyChanged
    {
        private bool _isCalled;
        private int _order;

        /// <summary>编号，从 1 开始，全量重写名单时重新按顺序赋值。</summary>
        public int Id { get; set; }

        /// <summary>姓名。</summary>
        public string Name { get; set; }

        /// <summary>小组号（数字），未分组为 0。</summary>
        public int GroupNumber { get; set; }

        /// <summary>是否组长。每组最多一名组长；单人群组自动成为组长。</summary>
        public bool IsLeader { get; set; }

        /// <summary>是否已被点过名。</summary>
        public bool IsCalled
        {
            get { return _isCalled; }
            set
            {
                if (_isCalled == value)
                {
                    return;
                }
                _isCalled = value;
                OnPropertyChanged("IsCalled");
            }
        }

        /// <summary>点名次序，从 1 开始；未点名为 0。</summary>
        public int Order
        {
            get { return _order; }
            set
            {
                if (_order == value)
                {
                    return;
                }
                _order = value;
                OnPropertyChanged("Order");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
