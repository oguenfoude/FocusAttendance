import io

p = r"D:\FocusAttendance\src\FocusAttendance.App\Views\DevicesPage.xaml"
t = io.open(p, encoding="utf-8").read()

block = '''                    <!-- Full SDK diagnostic -->
                    <Border Style="{StaticResource DrawerSection}">
                        <StackPanel>
                            <TextBlock Text="{DynamicResource Devices.DumpTitle}" Style="{StaticResource SectionTitle}" Margin="0,0,0,10" />
                            <TextBlock Text="{DynamicResource Devices.DumpHint}" Style="{StaticResource Caption}"
                                       Margin="0,0,0,10" TextWrapping="Wrap" />
                            <Button Style="{StaticResource SecondaryAction}" Command="{Binding DumpInfoCommand}"
                                    AutomationProperties.AutomationId="DeviceDumpButton">
                                <StackPanel Orientation="Horizontal">
                                    <ui:SymbolIcon Symbol="Info24" FontSize="13" Margin="0,0,6,0" />
                                    <TextBlock Text="{DynamicResource Devices.DumpAction}" />
                                </StackPanel>
                            </Button>
                            <Border Background="{DynamicResource PanelAltBrush}" CornerRadius="8" Padding="10" Margin="0,10,0,0"
                                    Visibility="{Binding HasInfoDump, Converter={StaticResource BoolToVisibility}}">
                                <ScrollViewer MaxHeight="260" VerticalScrollBarVisibility="Auto">
                                    <TextBlock Text="{Binding InfoDump}" Style="{StaticResource MonoText}" TextWrapping="Wrap" />
                                </ScrollViewer>
                            </Border>
                        </StackPanel>
                    </Border>

'''

marker = "                    <!-- Hardware -->"
if "DeviceDumpButton" not in t:
    if marker not in t:
        raise SystemExit("hardware marker not found")
    t = t.replace(marker, block + marker, 1)
    io.open(p, "w", encoding="utf-8", newline="").write(t)
    print("devices xaml patched")
else:
    print("already patched")

# localization
fr_add = '''    <sys:String x:Key="Devices.DumpTitle">Diagnostic complet (SDK)</sys:String>
    <sys:String x:Key="Devices.DumpHint">Lit tout ce que le terminal expose : numéro de série, code produit, micrologiciel, plateforme, MAC, horloge, capacités et compteurs.</sys:String>
    <sys:String x:Key="Devices.DumpAction">Lire toutes les informations</sys:String>
'''
ar_add = '''    <sys:String x:Key="Devices.DumpTitle">الفحص الكامل (SDK)</sys:String>
    <sys:String x:Key="Devices.DumpHint">يقرأ كل ما يقدمه الجهاز: الرقم التسلسلي، رمز المنتج، النسخة الثابتة، النظام، عنوان MAC، الساعة، السعات والعدادات.</sys:String>
    <sys:String x:Key="Devices.DumpAction">قراءة كل المعلومات</sys:String>
'''

for path, add, anchor in (
    (r"D:\FocusAttendance\src\FocusAttendance.App\Resources\Lang.fr.xaml", fr_add, '<sys:String x:Key="Devices.Console">'),
    (r"D:\FocusAttendance\src\FocusAttendance.App\Resources\Lang.ar.xaml", ar_add, '<sys:String x:Key="Devices.Console">'),
):
    x = io.open(path, encoding="utf-8").read()
    if "Devices.DumpTitle" in x:
        print("already:", path)
        continue
    idx = x.find(anchor)
    if idx < 0:
        raise SystemExit("anchor missing in " + path)
    x = x[:idx] + add + x[idx:]
    io.open(path, "w", encoding="utf-8", newline="").write(x)
    print("lang patched:", path)
