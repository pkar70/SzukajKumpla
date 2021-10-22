Imports Windows.ApplicationModel.Background

Public NotInheritable Class Czatownik
    Implements Background.IBackgroundTask

    Private Shared sLastId As String
    Public Sub Run(taskInstance As IBackgroundTaskInstance) Implements IBackgroundTask.Run
        Dim oTrigDet As Windows.Devices.Enumeration.DeviceWatcherTriggerDetails =
        TryCast(taskInstance.TriggerDetails, Windows.Devices.Enumeration.DeviceWatcherTriggerDetails)

        If oTrigDet Is Nothing Then
            DebugOut("oTrigDet Is Nothing")
            Return
        End If
        If oTrigDet.DeviceWatcherEvents Is Nothing Then
            DebugOut("oTrigDet.DeviceWatcherEvents Is Nothing")
            Return
        End If

        For Each oEvent As Windows.Devices.Enumeration.DeviceWatcherEvent In oTrigDet.DeviceWatcherEvents
            DebugOut("Device " & oEvent.DeviceInformation.Id & ", event: " & oEvent.Kind.ToString)
            If oEvent.Kind <> Windows.Devices.Enumeration.DeviceWatcherEventKind.Add Then Continue For

            If sLastId = oEvent.DeviceInformation.Id Then Continue For

            sLastId = oEvent.DeviceInformation.Id
            MakeToast(sLastId)

        Next

        ' wedle mLastUMAC, nie powtarzamy
    End Sub

    Public Function ToHexBytesString(iVal As ULong) As String
        Dim sTmp As String = String.Format("{0:X}", iVal)
        If sTmp.Length Mod 2 <> 0 Then sTmp = "0" & sTmp

        Dim sRet As String = ""
        Dim bDwukrop As Boolean = False

        While sTmp.Length > 0
            If bDwukrop Then sRet &= ":"
            bDwukrop = True
            sRet = sRet & sTmp.Substring(0, 2)
            sTmp = sTmp.Substring(2)
        End While

        ' gniazdko BT18, daje 15:A6:00:E8:07 (bez 00:)
        ' 71:0A:22:CD:4F:20
        ' 12345678901234567
        If sRet.Length < 17 Then sRet = "00:" & sRet
        If sRet.Length < 17 Then sRet = "00:" & sRet


        Return sRet
    End Function


    Public Function GetSettingsString(sName As String) As String
        Dim sTmp As String = ""

        With Windows.Storage.ApplicationData.Current
            If .LocalSettings.Values.ContainsKey(sName) Then
                sTmp = .LocalSettings.Values(sName).ToString
            End If
        End With

        Return sTmp

    End Function
    Public Sub SetSettingsString(sName As String, sValue As String)
        Try
            Windows.Storage.ApplicationData.Current.LocalSettings.Values(sName) = sValue
        Catch ex As Exception
            ' jesli przepełniony bufor (za długa zmienna) - nie zapisuj dalszych błędów
        End Try
    End Sub
    Public Sub DebugOut(sMsg As String)
        Debug.WriteLine("--PKAR---:    " & sMsg)
    End Sub

    Public Shared Sub MakeToast(sText As String)
        ' lekko zmodyfikowane o guziki, z FilteredRSS
        Dim sVisual As String = "<visual><binding template='ToastGeneric'><text>" & sText
        sVisual = sVisual & "</text></binding></visual>"

        Dim oXml = New Windows.Data.Xml.Dom.XmlDocument
        oXml.LoadXml("<toast>" & sVisual & "</toast>")

        Dim oToast = New Windows.UI.Notifications.ToastNotification(oXml)

        Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().Show(oToast)
    End Sub


End Class
