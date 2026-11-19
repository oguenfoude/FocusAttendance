$p = 'D:\FocusAttendance\src\FocusAttendance.App\Views\DevicesPage.xaml'
$t = [System.IO.File]::ReadAllText($p)

$startMarker = '        <!-- Full identity of the selected terminal -->'
$endMarker = '        <!-- Console drawer -->'
$start = $t.IndexOf($startMarker)
$end = $t.IndexOf($endMarker)
if ($start -lt 0 -or $end -le $start) { throw 'markers not found' }

$block = @'
        <!-- Full identity of the selected terminal -->
        <Border Grid.Row="4" Style="{StaticResource FluentCard}" Padding="18,14" Margin="0,12,0,0"
                Visibility="{Binding HasSelection, Converter={StaticResource BoolToVisibility}}">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="Auto" />
                </Grid.RowDefinitions>

                <Grid Grid.Row="0">
                    <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" VerticalAlignment="Center">
                        <ui:SymbolIcon Symbol="HardDrive24" FontSize="15" Foreground="{DynamicResource PrimaryBrush}" />
                        <TextBlock Text="{Binding SelectedDeviceTitle}" Style="{StaticResource SectionTitle}" Margin="8,0,0,0" VerticalAlignment="Center" />
                        <Border Style="{StaticResource Pill}" Margin="8,0,0,0">
                            <TextBlock Text="{Binding SelectedDevice.StatusText}" Style="{StaticResource PillText}" />
                        </Border>
                        <TextBlock Text="{Binding SelectedDevice.LatencyText}" Style="{StaticResource Caption}" Margin="8,0,0,0" VerticalAlignment="Center" />
                        <TextBlock Text="{Binding SelectedDevice.Endpoint}" Style="{StaticResource Caption}" Margin="12,0,0,0" VerticalAlignment="Center" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center">
                        <Button Style="{StaticResource SecondaryAction}" Command="{Binding SyncSelectedCommand}" Margin="8,0,0,0"
                                AutomationProperties.AutomationId="DeviceSyncSelectedButton">
                            <StackPanel Orientation="Horizontal">
                                <ui:SymbolIcon Symbol="Clock24" FontSize="13" Margin="0,0,6,0" />
                                <TextBlock Text="{DynamicResource Devices.SyncClock}" />
                            </StackPanel>
                        </Button>
                        <Button Style="{StaticResource SecondaryAction}" Command="{Binding PullSelectedCommand}" Margin="8,0,0,0"
                                AutomationProperties.AutomationId="DevicePullSelectedButton">
                            <StackPanel Orientation="Horizontal">
                                <ui:SymbolIcon Symbol="ArrowDownload24" FontSize="13" Margin="0,0,6,0" />
                                <TextBlock Text="{DynamicResource Devices.Pull}" />
                            </StackPanel>
                        </Button>
                        <Button Style="{StaticResource PrimaryAction}" Command="{Binding OpenDeviceCommand}"
                                CommandParameter="{Binding SelectedDevice}"
                                AutomationProperties.AutomationId="DeviceOpenConsoleButton">
                            <StackPanel Orientation="Horizontal">
                                <ui:SymbolIcon Symbol="Settings24" FontSize="13" Margin="0,0,6,0" />
                                <TextBlock Text="{DynamicResource Devices.Console}" />
                            </StackPanel>
                        </Button>
                    </StackPanel>
                </Grid>

                <Grid Grid.Row="1" Margin="0,12,0,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0">
                        <TextBlock Text="{DynamicResource Devices.Serial}" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedDevice.Serial}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" TextTrimming="CharacterEllipsis" />
                    </StackPanel>
                    <StackPanel Grid.Column="1">
                        <TextBlock Text="{DynamicResource Devices.Model}" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedDevice.ModelName}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" TextTrimming="CharacterEllipsis" />
                    </StackPanel>
                    <StackPanel Grid.Column="2">
                        <TextBlock Text="{DynamicResource Devices.Firmware}" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedDevice.Firmware}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" TextTrimming="CharacterEllipsis" />
                    </StackPanel>
                    <StackPanel Grid.Column="3">
                        <TextBlock Text="MAC" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedDevice.Mac}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" TextTrimming="CharacterEllipsis" />
                    </StackPanel>
                    <StackPanel Grid.Column="4">
                        <TextBlock Text="Platform" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedDevice.Platform}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" TextTrimming="CharacterEllipsis" />
                    </StackPanel>
                </Grid>

                <Grid Grid.Row="2" Margin="0,10,0,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0">
                        <TextBlock Text="FP / Face" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedDevice.BiometricsText}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" />
                    </StackPanel>
                    <StackPanel Grid.Column="1">
                        <TextBlock Text="{DynamicResource Devices.Clock}" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedDevice.ClockText}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" />
                    </StackPanel>
                    <StackPanel Grid.Column="2">
                        <TextBlock Text="{DynamicResource Devices.LastSeen}" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedDevice.LastSeenText}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" />
                    </StackPanel>
                    <StackPanel Grid.Column="3">
                        <TextBlock Text="{DynamicResource Devices.LastPull}" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedDevice.LastPullText}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" />
                    </StackPanel>
                    <StackPanel Grid.Column="4">
                        <TextBlock Text="{DynamicResource Devices.CommKey}" Style="{StaticResource FieldLabel}" />
                        <TextBlock Text="{Binding SelectedDevice.Model.CommKey}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12.5" />
                    </StackPanel>
                </Grid>

                <TextBlock Grid.Row="3" Text="{Binding SelectedDevice.FullStatusText}" Style="{StaticResource Caption}"
                           TextWrapping="Wrap" Margin="0,10,0,0" HorizontalAlignment="Right" MaxWidth="1200" />
            </Grid>
        </Border>

'@

$t = $t.Substring(0, $start) + $block + $t.Substring($end)
[System.IO.File]::WriteAllText($p, $t, [System.Text.UTF8Encoding]::new($false))
'devices details card rewritten'
