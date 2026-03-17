Imports System
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Drawing2D

' Transparency support for reels requires two things:
' 1. Painting the reel image into stable client coordinates instead of the clip rectangle.
' 2. Keeping animation state changes out of OnPaint so extra transparent repaints
'    do not leave the reel stuck on an intermediate frame.
Public Class B2SReelBox

    Inherits B2SBaseBox

    Public Enum eScoreType
        NotUsed = 0
        Scores = 1
        Credits = 2
    End Enum

    Public Class ReelRollOverEventArgs
        Inherits EventArgs

        Public Digit As Integer = 0

        Public Sub New(ByVal _digit As Integer)
            Digit = _digit
        End Sub
    End Class
    Public Event ReelRollOver(ByVal sender As Object, ByVal e As ReelRollOverEventArgs)

    Private timer As Timer = Nothing
    Private cTimerInterval As Integer = 101

    Private isLED As Boolean = False

    Private length As Integer = 1
    Private initValue As String = "0"
    Private reelindex As String = String.Empty

    ' Intermediate-frame state is tracked explicitly so paint remains read-only.
    Private totalIntermediates As Integer = 0
    Private currentIntermediateIndex As Integer = 0

    ' Dispose the reel timer and detach handlers when the control is released.
    Protected Overrides Sub Dispose(disposing As Boolean)
        MyBase.Dispose(disposing)
        On Error Resume Next
        If disposing Then
            If timer IsNot Nothing Then
                timer.Stop()
                RemoveHandler timer.Tick, AddressOf ReelAnimationTimer_Tick
                timer.Dispose()
            End If
            timer = Nothing
        End If
    End Sub

    ' Draw the current reel frame. This method only renders the already-selected
    ' frame and must not advance animation state because transparent controls can
    ' be repainted more often than opaque ones.
    Protected Overrides Sub OnPaint(ByVal e As System.Windows.Forms.PaintEventArgs)

        If Not String.IsNullOrEmpty(reelindex) Then
            Dim reelBounds As New Rectangle(0, 0, Me.Width, Me.Height)
            Dim image As Image = Nothing

            If currentIntermediateIndex > 0 Then
                Dim intermediateImageKey As String = GetIntermediateImageKey(reelindex, currentIntermediateIndex)
                If CurrentIntermediateImages().ContainsKey(intermediateImageKey) Then
                    image = CurrentIntermediateImages()(intermediateImageKey)
                End If
            End If

            If image Is Nothing Then
                Dim imageKey As String = GetImageKey(reelindex)
                If CurrentImages().ContainsKey(imageKey) Then
                    image = CurrentImages()(imageKey)
                End If
            End If

            If image IsNot Nothing Then
                e.Graphics.DrawImage(image, reelBounds)
            End If
        End If

    End Sub
    ' When the reel uses a transparent background, ask the parent to paint the
    ' area underneath the reel first so transparent pixels show the backglass
    ' rather than an empty or stale surface.
    Protected Overrides Sub OnPaintBackground(ByVal pevent As System.Windows.Forms.PaintEventArgs)

        If Me.BackColor = Color.Transparent AndAlso Me.Parent IsNot Nothing Then
            Dim state As GraphicsState = pevent.Graphics.Save()
            Try
                pevent.Graphics.TranslateTransform(-Me.Left, -Me.Top)
                Dim parentBounds As New Rectangle(Me.Left, Me.Top, Me.Width, Me.Height)
                Dim parentEvent As New PaintEventArgs(pevent.Graphics, parentBounds)
                Me.InvokePaintBackground(Me.Parent, parentEvent)
                Me.InvokePaint(Me.Parent, parentEvent)
            Finally
                pevent.Graphics.Restore(state)
            End Try
        Else
            MyBase.OnPaintBackground(pevent)
        End If

    End Sub

    ' Initialize the reel for transparent rendering and timer-driven animation.
    Public Sub New()

        ' set some styles
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.DoubleBuffer Or ControlStyles.SupportsTransparentBackColor, True)

        Me.DoubleBuffered = True

        ' let transparent reel art render over the backglass
        Me.BackColor = Color.Transparent

        ' create timer
        timer = New Timer()
        timer.Interval = CInt(_RollingInterval / 2)
        AddHandler timer.Tick, AddressOf ReelAnimationTimer_Tick

    End Sub

    ' Defensive cleanup for cases where the control is disposed by WinForms.
    Private Sub B2SReelBox_Disposed(sender As Object, e As System.EventArgs) Handles Me.Disposed
        On Error Resume Next
        If timer IsNot Nothing Then
            timer.Stop()
            RemoveHandler timer.Tick, AddressOf ReelAnimationTimer_Tick
            timer.Dispose()
            timer = Nothing
        End If
    End Sub

    ' Advance the reel animation. Intermediate images are stepped here so the
    ' visible frame changes only on timer ticks, never as a side effect of paint.
    Private Sub ReelAnimationTimer_Tick(ByVal sender As Object, ByVal e As System.EventArgs)

        If currentIntermediateIndex > 0 AndAlso currentIntermediateIndex < totalIntermediates Then
            currentIntermediateIndex += 1
            Me.Invalidate()
        Else
            ' add one reel step
            _CurrentText += 1
            If _CurrentText > 9 Then
                _CurrentText = 0
                RaiseEvent ReelRollOver(Me, New ReelRollOverEventArgs(ID))
            End If

            reelindex = ConvertText(_CurrentText)

            ' play sound and redraw reel
            Try
                If Sound() IsNot Nothing Then
                    My.Computer.Audio.Play(Sound(), AudioPlayMode.Background)
                ElseIf SoundName() = "stille" Then
                    ' no sound
                Else
                    My.Computer.Audio.Play(My.Resources.EMReel, AudioPlayMode.Background)
                End If
            Catch
            End Try

            If _CurrentText = _Text OrElse _Text >= 10 Then
                timer.Stop()
                ResetIntermediateState()
                timer.Interval = CInt(_RollingInterval / 2)
            Else
                totalIntermediates = CountIntermediateImages(reelindex)
                currentIntermediateIndex = If(totalIntermediates > 0, 1, 0)
                timer.Interval = CInt(_RollingInterval / (Math.Max(totalIntermediates, 1) + 1))
            End If

            Me.Invalidate()

        End If

    End Sub

    Public Property SetID() As Integer

    Private _ReelType As String
    Public Property ReelType() As String
        Get
            Return _ReelType
        End Get
        Set(ByVal value As String)
            reelindex = "0"
            If value.Substring(value.Length - 1, 1) = "_" Then
                length = 2
                reelindex = "00"
                value = value.Substring(0, value.Length - 1)
            End If
            If value.StartsWith("LED", StringComparison.CurrentCultureIgnoreCase) OrElse value.StartsWith("ImportedLED", StringComparison.CurrentCultureIgnoreCase) Then
                isLED = True
                reelindex = "Empty"
                initValue = "Empty"
                _Text = -1
            End If
            _ReelType = value
        End Set
    End Property

    Public Property SoundName() As String = String.Empty
    Public Property Sound() As Byte() = Nothing

    Public Property ScoreType() As eScoreType = eScoreType.NotUsed

    Public Property GroupName() As String = String.Empty

    Private _Illuminated As Boolean
    ' Switch between lit and unlit image sets and clear intermediate animation
    ' state so a lighting change cannot leave a stale partial frame onscreen.
    Public Property Illuminated() As Boolean
        Get
            Return _Illuminated
        End Get
        Set(ByVal value As Boolean)
            If _Illuminated <> value Then
                _Illuminated = value
                ResetIntermediateState()
                Me.Invalidate()
            End If
        End Set
    End Property

    Private _Value As Integer = 0
    ' Set a raw segment value and redraw the reel using the mapped reel index.
    Public Property Value(Optional ByVal refresh As Boolean = False) As Integer
        Get
            Return _Value
        End Get
        Set(ByVal value As Integer)
            If _Value <> value OrElse refresh Then
                _Value = value
                reelindex = ConvertValue(_Value)
                ResetIntermediateState()
                Me.Invalidate()
            End If
        End Set
    End Property

    Private _CurrentText As Integer = 0
    Private _Text As Integer = 0
    ' Set the target reel digit. For mechanical reels this starts timer-driven
    ' animation; for direct updates it jumps straight to the final frame.
    Public Shadows Property Text(Optional ByVal AnimateReelChange As Boolean = True) As Integer
        Get
            Return _Text
        End Get
        Set(ByVal value As Integer)
            If value >= 0 Then
                If _Text <> value Then
                    _Text = value
                    If AnimateReelChange AndAlso Not isLED Then
                        timer.Stop()
                        totalIntermediates = CountIntermediateImages(reelindex)
                        currentIntermediateIndex = If(totalIntermediates > 0, 1, 0)
                        timer.Interval = CInt(_RollingInterval / (Math.Max(totalIntermediates, 1) + 1))
                        timer.Start()
                        Me.Invalidate()
                    Else
                        ResetIntermediateState()
                        reelindex = ConvertText(_Text)
                        Me.Invalidate()
                    End If
                End If
            End If
        End Set
    End Property
    Public ReadOnly Property CurrentText() As Integer
        Get
            Return _CurrentText
        End Get
    End Property

    Private _RollingInterval As Integer = cTimerInterval
    Public Property RollingInterval() As Integer
        Get
            Return _RollingInterval
        End Get
        Set(ByVal value As Integer)
            If _RollingInterval <> value Then
                _RollingInterval = value
                If _RollingInterval < 10 Then _RollingInterval = cTimerInterval
            End If
        End Set
    End Property

    Public ReadOnly Property IsInReelRolling() As Boolean
        Get
            Return (currentIntermediateIndex > 0)
        End Get
    End Property
    Public ReadOnly Property IsInAction() As Boolean
        Get
            Return timer.Enabled
        End Get
    End Property

    ' Convert a segment-style numeric value to the reel image suffix expected by
    ' imported LED and reel image sets.
    Private Function ConvertValue(ByVal value As Integer) As String
        Dim ret As String = initValue
        ' remove the "," from the 7-segmenter
        If value >= 128 AndAlso value <= 255 Then
            value -= 128
        End If
        ' map value
        If value > 0 Then
            Select Case value
                ' 7-segment stuff
                Case 63
                    ret = "0"
                Case 6
                    ret = "1"
                Case 91
                    ret = "2"
                Case 79
                    ret = "3"
                Case 102
                    ret = "4"
                Case 109
                    ret = "5"
                Case 125
                    ret = "6"
                Case 7
                    ret = "7"
                Case 127
                    ret = "8"
                Case 111
                    ret = "9"
                Case Else
                    'additional 10-segment stuff
                    Select Case value
                        Case 768
                            ret = "1"
                        Case 124
                            ret = "6"
                        Case 103
                            ret = "9"
                            'Case Else
                            '    Debug.WriteLine(_Value)
                    End Select
            End Select
        End If
        Return If(length = 2, "0", "") & ret
    End Function

    ' Convert a plain digit to the padded reel image suffix.
    Private Function ConvertText(ByVal text As Integer) As String
        Dim ret As String = String.Empty
        ret = "00" & text.ToString()
        ret = ret.Substring(ret.Length - length, length)
        Return ret
    End Function

    ' Resolve the active base image dictionary for the current illumination state.
    Private Function CurrentImages() As Generic.Dictionary(Of String, Image)
        Return If(_Illuminated, B2SData.ReelIlluImages, B2SData.ReelImages)
    End Function

    ' Resolve the active intermediate image dictionary for the current illumination state.
    Private Function CurrentIntermediateImages() As Generic.Dictionary(Of String, Image)
        Return If(_Illuminated, B2SData.ReelIntermediateIlluImages, B2SData.ReelIntermediateImages)
    End Function

    ' Build the base image key used by the reel image collections.
    Private Function GetImageKey(ByVal currentReelIndex As String) As String
        Return _ReelType & "_" & currentReelIndex & If(SetID > 0 AndAlso _Illuminated, "_" & SetID.ToString(), "")
    End Function

    ' Build the key for a numbered intermediate frame.
    Private Function GetIntermediateImageKey(ByVal currentReelIndex As String, ByVal index As Integer) As String
        Return GetImageKey(currentReelIndex) & "_" & index.ToString()
    End Function

    ' Count available intermediate frames for the supplied reel index.
    Private Function CountIntermediateImages(ByVal currentReelIndex As String) As Integer
        Dim count As Integer = 0
        Do While CurrentIntermediateImages().ContainsKey(GetIntermediateImageKey(currentReelIndex, count + 1))
            count += 1
        Loop
        Return count
    End Function

    ' Clear partial-frame state so the next paint shows a stable base frame unless
    ' the timer explicitly starts a new animated transition.
    Private Sub ResetIntermediateState()
        totalIntermediates = 0
        currentIntermediateIndex = 0
    End Sub

End Class
