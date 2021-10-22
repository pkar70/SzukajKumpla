
Public NotInheritable Class MainPage
    Inherits Page
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        ProgRingInit(True, True)
        RegisterTrigger()

    End Sub

    Private Async Sub uiGo_Click(sender As Object, e As RoutedEventArgs)
        ' StartScan()

        Dim oGniazdka As Windows.Devices.Enumeration.DeviceInformationCollection
        oGniazdka = Await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.Bluetooth.BluetoothDevice.GetDeviceSelectorFromPairingState(True))
        DebugOut("Sparowane urządzenia:")
        For Each oItem As Windows.Devices.Enumeration.DeviceInformation In oGniazdka
            DebugOut(oItem.Name & " " & oItem.Kind.ToString)
        Next



    End Sub

    Private Sub RegisterTrigger()

        UnregisterTriggers("CzatujBT_")

        Dim oAdv As Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisement =
            New Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisement
        oAdv.ServiceUuids.Add(New Guid("6E9E7830-F4C7-4717-B0D8-525D30181121"))

        Dim oFilt As Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementFilter =
            New Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementFilter
        oFilt.Advertisement = oAdv

        Dim oTrigger As Background.BluetoothLEAdvertisementWatcherTrigger =
            New Background.BluetoothLEAdvertisementWatcherTrigger
        oTrigger.AdvertisementFilter = oFilt


        Dim oTaskBuilder As Background.BackgroundTaskBuilder = New Background.BackgroundTaskBuilder()
        oTaskBuilder.SetTrigger(oTrigger)
        oTaskBuilder.Name = "CzatujBT_ServiceFound"
        oTaskBuilder.TaskEntryPoint = "CzatujWtle.Czatownik"

        Dim oRet As Background.BackgroundTaskRegistration
        oRet = oTaskBuilder.Register()

    End Sub

#Region "BT scanning"

    Public moBLEWatcher As Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementWatcher = Nothing
    Private moTimer As DispatcherTimer
    Private miTimerCnt As Integer = 30
    Private msAllDevNames As String = ""

    Private Sub TimerTick(sender As Object, e As Object)
        DebugOut("TimerTick(), miTimer = " & miTimerCnt)
        ProgRingInc()
        miTimerCnt -= 1
        If miTimerCnt > 0 Then Return

        DebugOut("TimerTick(), stopping...")
        StopScan()

    End Sub


    Private Async Sub StartScan()
        DebugOut("StartScan()")
        If Await NetIsBTavailableAsync(False) < 1 Then Return

        moTimer = New DispatcherTimer()
        AddHandler moTimer.Tick, AddressOf TimerTick
        moTimer.Interval = New TimeSpan(0, 0, 1)
        miTimerCnt = 10 ' sekund na szukanie, ale z progress bar
        moTimer.Start()

        ProgRingShow(True, False, 0, miTimerCnt)

        ' App.moDevicesy = New Collection(Of JedenDevice) - nieprawda! korzystamy z dotychczasowych danych!
        'uiGoPair.IsEnabled = False
        ScanSinozeby()
    End Sub

    Private Async Function StopScan() As Task
        DebugOut("StopScan()")

        If moTimer IsNot Nothing Then
            DebugOut("StopScan - stopping Timer")
            moTimer.Stop()
        End If

        If moBLEWatcher IsNot Nothing AndAlso
            moBLEWatcher.Status <> Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementWatcherStatus.Stopped Then
            DebugOut("StopScan - stopping moBLEWatcher")
            moBLEWatcher.Stop()
        End If

        'If uiGoPair IsNot Nothing Then uiGoPair.IsEnabled = True

        ' dodanie znalezionych
        For Each sMAC As String In msAllDevNames.Split("|")
            If sMAC.Length < 10 Then Continue For  ' z podzialu mogą być jakieś empty, i tym podobne

            ' Await AddNewDevice(sMAC)
        Next

        'Await App.gmGniazdka.SaveAsync(False)

        ' podczas OnUnload - już nie będzie czego wyłączać; choć powinno zadziałać zabezpieczenie w ProgRing
        'If uiGoPair IsNot Nothing Then
        '    ShowLista()
        ProgRingShow(False)
        'End If
    End Function

    Private Sub ScanSinozeby()
        DebugOut("ScanSinozeby() starting")
        ' https://stackoverflow.com/questions/40950482/search-for-devices-in-range-of-bluetooth-uwp
        'przekopiowane z RGBLed
        moBLEWatcher = New Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementWatcher
        moBLEWatcher.ScanningMode = 1   ' 1: żąda wysłania adv (czyli tryb aktywny)
        AddHandler moBLEWatcher.Received, AddressOf BTwatch_Received
        moBLEWatcher.Start()
    End Sub


    Private Async Sub BTwatch_Received(sender As Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementWatcher,
                                   args As Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementReceivedEventArgs)

        Dim sTxt As String = "New BT device, Mac=" & args.BluetoothAddress.ToHexBytesString & ", locName=" & args.Advertisement.LocalName

        '' sprawdzenie czy to pasuje do naszego urządzenia
        'If Not args.Advertisement.LocalName = "BT18" Then
        '    Return
        'End If

        DebugOut(sTxt)  ' dopiero tu, zeby ignorowal te inne cosiki BT co sie pojawiają (22:7D:4F:50:B2:E7, C4:98:5C:D4:2E:A7, 32:75:ED:BF:BC:A7)

        ' wewnetrzne zabezpieczenie przed powtorkami - bo czesto wyskakuje blad przy ForEach, ze sie zmienila Collection
        Dim sNewAddr As String = "|" & args.BluetoothAddress.ToHexBytesString & "|"
        If msAllDevNames.Contains(sNewAddr) Then
            DebugOut("ale juz taki adres mam")
            Return
        End If
        msAllDevNames = msAllDevNames & sNewAddr

    End Sub




#End Region



End Class
