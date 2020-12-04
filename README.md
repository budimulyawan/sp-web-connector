# sp-web-connector

This project is showing how you can create Smartpoint plugin that create a bridge between web application and Smartpoint desktop.
![alt text](https://budimulyawan.github.io/sp-web-connector/sp-web-connector.gif "Demo Smartpoint web connector")

### Communication from Smartpoint to WebApplication
We use Smartpoint SDK to get the information then inject the information using javascript as example below

```C#
string windowTitle = GetWindowTitle();
var window = CreateWebBrowserWindow(new Uri(url), BROWSER_ID, windowTitle);
var wb = window.Content as SmartBrowserControl;
var bf = UIHelper.Instance.CurrentTEControl.Connection.CommunicationFactory.RetrieveCurrentBookingFile();
var names = !String.IsNullOrEmpty(bf?.RecordLocator) ?
    bf.Passengers.Select(p => p.FirstName + " " + p.LastName)
    .Aggregate((a, b) => a + System.Environment.NewLine + b) : String.Empty;

wb.WebBrowserControl.FrameLoadEnd += (sender, eventArgs) =>
{
    var htmlElement = GetConfigValue("PasteHTMLElement");

    var js = String.Format("document.querySelector('{0}').value = '{1}'", htmlElement, names);

    var host = eventArgs.Browser.GetHost();
    if (host != null)
    {
        host.ShowDevTools();
    }
    //Wait for the MainFrame to finish loading
    if (eventArgs.Frame.IsMain)
    {
        eventArgs.Frame.ExecuteJavaScriptAsync(js);
    }
};
window.Show();
```
### Communication from WebApplication to Smartpoint
First, we need to expose C# class that is accessible from javascript.
```C#
CefSharpSettings.LegacyJavascriptBindingEnabled = true; // change this to new binding technique https://github.com/cefsharp/CefSharp/issues/2246
wb = new SmartBrowserControl();
// set background to white otherwise it will inherit SP background color like bluish.
var converter = new System.Windows.Media.BrushConverter();
var brush = (Brush)converter.ConvertFromString("#FFFFFFFF");
wb.Background = brush;
try
{
    wb.RegisterJsObject("spHelper", this);
}
catch (Exception ex)
{

}
window.Show();
```

Then from javascript we can access this "spHelper" object like below
```javascript
spHelper.sendTerminalCommand("NP." + $('#txtval').val());
var response = spHelper.sendXmlRequest($('#xmlRequest').val());
```
