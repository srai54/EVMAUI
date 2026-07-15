namespace EVSwap.Mobile.Helpers;

public static class Constants
{
    public const string ApiBaseUrl = "http://localhost:5238";

    public static class Routes
    {
        public const string Login = "login";
        public const string Register = "register";
        public const string ForgotPassword = "forgotpassword";
        public const string Dashboard = "dashboard";
        public const string Stations = "stations";
        public const string StationDetail = "stationdetail";
        public const string BatterySwap = "batteryswap";
        public const string SwapRequest = "swaprequest";
        public const string QRScan = "qrscan";
        public const string Trips = "trips";
        public const string Wallet = "wallet";
        public const string AddMoney = "addmoney";
        public const string Notifications = "notifications";
        public const string Profile = "profile";
        public const string Settings = "settings";
        public const string AdminDashboard = "admindashboard";
        public const string UserManagement = "usermanagement";
        public const string FleetDashboard = "fleetdashboard";
        public const string MaintenanceDashboard = "maintenancedashboard";
    }

    public static class StorageKeys
    {
        public const string AuthToken = "auth_token";
        public const string RefreshToken = "refresh_token";
        public const string UserKey = "user_data";
        public const string BiometricEnabled = "biometric_enabled";
        public const string NotificationsEnabled = "notifications_enabled";
    }
}
