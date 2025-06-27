Imports System.IO
Imports System.Net.Http

Public Class Form1
    Private WithEvents _httpClient As New HttpClient()
    Private _stream As Stream = Nothing
    Private _buffer As Byte() = New Byte(4096) {}
    Private _imageBuffer As New MemoryStream()
    Private _image As Bitmap
    Private _isInFrame As Boolean = False
    Private _isDetectionRunning As Boolean = False ' Flag to check if detection is running


    Private Async Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Timer1.Interval = 100
        Timer1.Start()

        Try
            ' Connect to the Flask video stream
            Dim streamResponse As Stream = Await _httpClient.GetStreamAsync("http://localhost:5000/video_feed")
            _stream = streamResponse
            Console.WriteLine("Connected to video stream.")
        Catch ex As Exception
            MessageBox.Show("Error connecting to video stream: " & ex.Message)
            'Console.WriteLine("Error connecting to video stream: " & ex.Message)
        End Try
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If _stream IsNot Nothing And _isDetectionRunning Then
            Try
                ' Read data from the stream
                Dim bytesRead As Integer = _stream.Read(_buffer, 0, _buffer.Length)

                If bytesRead > 0 Then
                    ' Process the data by checking for start and end of JPEG frames
                    For i As Integer = 0 To bytesRead - 1
                        If _isInFrame Then
                            _imageBuffer.WriteByte(_buffer(i))

                            ' Check for end of JPEG frame (0xFF 0xD9 is the JPEG end marker)
                            If _buffer(i) = &HFF AndAlso i + 1 < bytesRead AndAlso _buffer(i + 1) = &HD9 Then
                                ' End of the frame found
                                _isInFrame = False

                                ' Process the complete frame
                                _imageBuffer.Seek(0, SeekOrigin.Begin)
                                _image = New Bitmap(_imageBuffer)
                                PictureBox1.Image = _image
                                _imageBuffer.SetLength(0) ' Reset the buffer for the next frame
                                Console.WriteLine("Frame displayed.")
                            End If
                        Else
                            ' Look for the start of a new JPEG frame (0xFF 0xD8 is the JPEG start marker)
                            If _buffer(i) = &HFF AndAlso i + 1 < bytesRead AndAlso _buffer(i + 1) = &HD8 Then
                                _isInFrame = True
                                _imageBuffer.SetLength(0) ' Reset buffer to start collecting new frame
                                _imageBuffer.WriteByte(_buffer(i))
                                _imageBuffer.WriteByte(_buffer(i + 1))
                                i += 1 ' Skip the next byte since it's part of the start marker
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                ' MessageBox.Show("Error reading from stream: " & ex.Message)
                Console.WriteLine("Error reading from stream: " & ex.Message)
            End Try
        End If
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Try
            Timer1.Stop()
            _httpClient.Dispose()
            _imageBuffer.Dispose()
            _stream.Dispose()
        Catch ex As Exception

        Finally
            Application.Exit()
        End Try

    End Sub

    Private Async Sub btnStartDetection_Click(sender As Object, e As EventArgs) Handles btnStartDetection.Click
        If Not _isDetectionRunning Then
            _isDetectionRunning = True
            Timer1.Start() ' Start the timer when detection is activated
            Try
                ' Connect to the Flask video stream
                Dim streamResponse As Stream = Await _httpClient.GetStreamAsync("http://localhost:5000/video_feed")
                _stream = streamResponse
                Console.WriteLine("Connected to video stream.")
            Catch ex As Exception
                MessageBox.Show("Error connecting to video stream: " & ex.Message)
                Console.WriteLine("Error connecting to video stream: " & ex.Message)
            End Try
        Else
            MessageBox.Show("Detection is already running.")
        End If
    End Sub
End Class
