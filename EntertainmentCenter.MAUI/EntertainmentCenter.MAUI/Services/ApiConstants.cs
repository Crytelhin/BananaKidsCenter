namespace EntertainmentCenter.Services;

public static class ApiConstants
{
    // При разработке: IP своего ноутбука
    // При деплое у клиента: http://ИМЯ-ПК-КЛИЕНТА.local:5000
    // Узнать имя ПК: команда hostname в PowerShell на сервере клиента
    public const string BaseUrl = "http://192.168.100.128:5000";
}
