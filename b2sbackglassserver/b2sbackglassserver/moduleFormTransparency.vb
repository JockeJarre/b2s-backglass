#Disable Warning BC42016, BC42017, BC42018, BC42019, BC42032
Imports System
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Module moduleFormTransparency

    Private ReadOnly conditionalTransparencyKey As Color = Color.FromArgb(255, 1, 0, 1)
    Private ReadOnly transparencyDebugLog As Log = New Log("B2SDebugLog")

    Public Sub EnableFormTransparencyIfNeeded(hostForm As Form, image As Image, Optional sourceDescription As String = Nothing)
        If hostForm Is Nothing OrElse image Is Nothing Then Return
        If Not B2SSettings.IsImageTransparencyEnabled Then Return
        If hostForm.TransparencyKey = conditionalTransparencyKey Then Return
        If Not ImageHasTransparentPixels(image) Then Return

        hostForm.AllowTransparency = True
        hostForm.BackColor = conditionalTransparencyKey
        hostForm.TransparencyKey = conditionalTransparencyKey
        WriteTransparencyLogEntry(hostForm, image, sourceDescription)
        hostForm.Invalidate()
    End Sub

    Private Sub WriteTransparencyLogEntry(hostForm As Form, image As Image, sourceDescription As String)
        transparencyDebugLog.IsLogOn = B2SSettings.B2SDebugLog
        transparencyDebugLog.WriteLogEntry(DateTime.Now & ": Conditional transparency enabled for form '" & hostForm.Name &
                                           If(String.IsNullOrEmpty(sourceDescription), String.Empty, "' from '" & sourceDescription) &
                                           "' using image " & image.Width & "x" & image.Height &
                                           " (pixel format: " & image.PixelFormat.ToString() & ")")
    End Sub

    Private Function ImageHasTransparentPixels(image As Image) As Boolean
        If image Is Nothing OrElse image.Width <= 0 OrElse image.Height <= 0 Then Return False

        Dim flags As Integer = image.Flags
        Dim mayContainTransparency As Boolean = ((flags And CInt(ImageFlags.HasAlpha)) <> 0) OrElse
                                             ((flags And CInt(ImageFlags.HasTranslucent)) <> 0) OrElse
                                             Image.IsAlphaPixelFormat(image.PixelFormat)
        If Not mayContainTransparency Then Return False

        Dim rect As New Rectangle(0, 0, image.Width, image.Height)
        Using scanBitmap As New Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb)
            Using graphics As Graphics = Graphics.FromImage(scanBitmap)
                graphics.DrawImage(image, rect)
            End Using

            Dim data As BitmapData = scanBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
            Try
                Dim stride As Integer = Math.Abs(data.Stride)
                Dim pixelsLength As Integer = stride * scanBitmap.Height
                Dim pixels(pixelsLength - 1) As Byte
                Marshal.Copy(data.Scan0, pixels, 0, pixelsLength)

                For index As Integer = 3 To pixels.Length - 1 Step 4
                    If pixels(index) < Byte.MaxValue Then
                        Return True
                    End If
                Next
            Finally
                scanBitmap.UnlockBits(data)
            End Try
        End Using

        Return False
    End Function

End Module