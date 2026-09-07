using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ParcelBuilder.Core.Models
{
    /// <summary>
    /// Represents an override for an individual parcel that deviates from the base parcel parameters.
    /// </summary>
    public class ParcelException : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _exceptionId = Guid.NewGuid().ToString("N");
        private ParcelSide _side = ParcelSide.SideA;
        private int _sequence = 1;
        private double? _customFrontage;
        private double? _customDepth;
        private string _customType = "Standard";
        private string _reason = string.Empty;

        public string ExceptionId
        {
            get => _exceptionId;
            set => SetProperty(ref _exceptionId, value);
        }

        /// <summary>
        /// Which side this parcel exception applies to.
        /// </summary>
        public ParcelSide Side
        {
            get => _side;
            set
            {
                if (SetProperty(ref _side, value))
                {
                    OnPropertyChanged(nameof(IsSideA));
                    OnPropertyChanged(nameof(IsSideB));
                    OnPropertyChanged(nameof(ParcelIdentifier));
                }
            }
        }

        public bool IsSideA
        {
            get => _side == ParcelSide.SideA;
            set { if (value) Side = ParcelSide.SideA; }
        }

        public bool IsSideB
        {
            get => _side == ParcelSide.SideB;
            set { if (value) Side = ParcelSide.SideB; }
        }

        /// <summary>
        /// 1-based sequence index of the parcel along its side.
        /// </summary>
        public int Sequence
        {
            get => _sequence;
            set
            {
                if (SetProperty(ref _sequence, value))
                {
                    OnPropertyChanged(nameof(ParcelIdentifier));
                }
            }
        }

        /// <summary>
        /// Formatted parcel identifier e.g. "A-02" or "B-05".
        /// </summary>
        public string ParcelIdentifier => $"{(Side == ParcelSide.SideA ? "A" : "B")}-{Sequence:D2}";

        /// <summary>
        /// Custom frontage in meters, or null to inherit BaseParcel frontage.
        /// </summary>
        public double? CustomFrontage
        {
            get => _customFrontage;
            set => SetProperty(ref _customFrontage, value);
        }

        /// <summary>
        /// Custom depth in meters, or null to inherit BaseParcel depth.
        /// </summary>
        public double? CustomDepth
        {
            get => _customDepth;
            set => SetProperty(ref _customDepth, value);
        }

        /// <summary>
        /// Custom parcel classification / type (e.g. "Corner", "Commercial", "Utility", "Irregular").
        /// </summary>
        public string CustomType
        {
            get => _customType;
            set => SetProperty(ref _customType, value);
        }

        /// <summary>
        /// Optional reason for exception (for auditing).
        /// </summary>
        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        public ParcelException Clone()
        {
            return new ParcelException
            {
                ExceptionId = this.ExceptionId,
                Side = this.Side,
                Sequence = this.Sequence,
                CustomFrontage = this.CustomFrontage,
                CustomDepth = this.CustomDepth,
                CustomType = this.CustomType,
                Reason = this.Reason
            };
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
