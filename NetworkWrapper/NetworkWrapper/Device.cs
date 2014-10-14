namespace NetworkWrapper
{
    using System;

    public class Device
    {
        private string _address;
        private string _description;
        private string _name;
        private string _netmask;

        public Device()
        {
            this._name = null;
            this._description = null;
            this._address = null;
            this._netmask = null;
        }

        public Device(string name, string description, string address, string netmask)
        {
            this._name = name;
            this._description = description;
            this._address = address;
            this._netmask = netmask;
        }

        public virtual string Address
        {
            get
            {
                return this._address;
            }
            set
            {
                this._address = value;
            }
        }

        public virtual string Description
        {
            get
            {
                return this._description;
            }
            set
            {
                this._description = value;
            }
        }

        public virtual string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
            }
        }

        public virtual string Netmask
        {
            get
            {
                return this._netmask;
            }
            set
            {
                this._netmask = value;
            }
        }
    }
}

