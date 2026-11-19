$p = 'D:\FocusAttendance\src\FocusAttendance.App\Views\LanScannerWindow.xaml'
$t = [System.IO.File]::ReadAllText($p)

$t = $t.Replace('Width="920" Height="660" MinWidth="780" MinHeight="560"', 'Width="1240" Height="740" MinWidth="940" MinHeight="600"')

$t = $t.Replace(@'
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
'@, @'
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
'@)

$t = $t.Replace('<DataGrid x:Name="ResultsGrid" Style="{StaticResource FlatGrid}" ItemsSource="{Binding Rows}"
                      Margin="4,0,4,8" AutoGenerateColumns="False" IsReadOnly="False">',
 '<DataGrid x:Name="ResultsGrid" Style="{StaticResource FlatGrid}" ItemsSource="{Binding Rows}"
                      SelectedItem="{Binding SelectedRow, Mode=TwoWay}"
                      Margin="4,0,4,8" AutoGenerateColumns="False" IsReadOnly="False">')

$details = @'
        <!-- Everything the terminal answered, for the selected row -->
        <Border Grid.Row="4" Style="{StaticResource FlatCard}" Padding="16,12" Margin="0,10,0,0"
                Visibility="{Binding HasSelection, Converter={StaticResource BoolToVisibility}}">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>
                <WrapPanel>
                    <StackPanel Margin="0,0,22,8" MinWidth="120">
                        <TextBlock Text="{DynamicResource Devices.Serial}" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedSerial}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" />
                    </StackPanel>
                    <StackPanel Margin="0,0,22,8" MinWidth="120">
                        <TextBlock Text="{DynamicResource Devices.Model}" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedModel}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" />
                    </StackPanel>
                    <StackPanel Margin="0,0,22,8" MinWidth="110">
                        <TextBlock Text="{DynamicResource Devices.Firmware}" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedFirmware}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" />
                    </StackPanel>
                    <StackPanel Margin="0,0,22,8" MinWidth="130">
                        <TextBlock Text="MAC" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedMac}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" />
                    </StackPanel>
                    <StackPanel Margin="0,0,22,8" MinWidth="110">
                        <TextBlock Text="Platform" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedPlatform}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" />
                    </StackPanel>
                    <StackPanel Margin="0,0,22,8" MinWidth="110">
                        <TextBlock Text="FP / Face" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedBiometrics}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" />
                    </StackPanel>
                    <StackPanel Margin="0,0,22,8" MinWidth="110">
                        <TextBlock Text="{DynamicResource Devices.Clock}" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedClock}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" />
                    </StackPanel>
                    <StackPanel Margin="0,0,22,8" MinWidth="90">
                        <TextBlock Text="{DynamicResource Devices.Users}" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedUsers}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" />
                    </StackPanel>
                    <StackPanel Margin="0,0,22,8" MinWidth="90">
                        <TextBlock Text="{DynamicResource Devices.Punches}" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedPunches}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" />
                    </StackPanel>
                    <StackPanel Margin="0,0,22,8" MinWidth="120">
                        <TextBlock Text="{DynamicResource App.Status}" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedStatusDetail}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" TextWrapping="Wrap" MaxWidth="420" />
                    </StackPanel>
                </WrapPanel>
                <StackPanel Grid.Column="1" VerticalAlignment="Bottom" Orientation="Horizontal" Margin="10,0,0,0">
                    <Button Style="{StaticResource SecondaryAction}" Content="{DynamicResource Scan.Retry}"
                            Click="OnRetrySelectedClick" Margin="0,0,8,0"
                            AutomationProperties.AutomationId="ScanRetrySelectedButton" />
                    <Button Style="{StaticResource PrimaryAction}" Content="{DynamicResource Scan.Add}"
                            Click="OnAddSelectedClick"
                            AutomationProperties.AutomationId="ScanAddSelectedButton" />
                </StackPanel>
            </Grid>
        </Border>

'@

$hint = @'
        <TextBlock Grid.Row="4" Style="{StaticResource Caption}" Margin="0,10,0,0"
                   Text="{DynamicResource Scan.Hint}" TextWrapping="Wrap" />
'@

$newHint = @'
        <TextBlock Grid.Row="5" Style="{StaticResource Caption}" Margin="0,10,0,0"
                   Text="{DynamicResource Scan.Hint}" TextWrapping="Wrap" />
'@

if (-not $t.Contains($hint)) { throw 'hint block not found' }
$t = $t.Replace($hint, $details + $newHint)

[System.IO.File]::WriteAllText($p, $t, [System.Text.UTF8Encoding]::new($false))
'patched scanner xaml'
