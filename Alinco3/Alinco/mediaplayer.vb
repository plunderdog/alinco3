Imports WMPLib

Friend Class mediaplayer
    Private WithEvents mPlayer As New WindowsMediaPlayerClass
    Private mSounds As New Dictionary(Of String, IWMPMedia)

    Public Sub playsoundfile(ByVal filename As String)
        Dim m As IWMPMedia
        If Not mSounds.ContainsKey(filename) Then
            m = mPlayer.newMedia(filename)
            mSounds.Add(filename, m)
        Else
            m = mSounds.Item(filename)
        End If

        mPlayer.currentMedia = m
        mPlayer.settings.autoStart = False
        Me.Play()
    End Sub

    Public Property Volume() As Integer
        Get
            Return mPlayer.volume
        End Get
        Set(ByVal value As Integer)
            mPlayer.volume = value
        End Set
    End Property

    Public Sub Play()
        If mPlayer.playState = WMPLib.WMPPlayState.wmppsReady Or _
           mPlayer.playState = WMPLib.WMPPlayState.wmppsStopped Or _
           mPlayer.playState = WMPLib.WMPPlayState.wmppsPaused Then
            mPlayer.controls.play()
        ElseIf mPlayer.playState = WMPLib.WMPPlayState.wmppsTransitioning Then
            ' If the player is in Transitioning state, do nothing
        Else
            ' mPlayer.controls.pause()
        End If
    End Sub

    Private Sub mPlayer_PlayStateChange(ByVal NewState As Integer) Handles mPlayer.PlayStateChange

    End Sub
End Class

