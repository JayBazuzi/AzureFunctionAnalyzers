
```cs
class UserGetter
{
    [Function("GetUsers")] // warning: Class name 'UserGetter' does not match function name 'GetUsers', expected 'GetUsersFunction'
    public void Run()
    {
        //...
    }
}
```