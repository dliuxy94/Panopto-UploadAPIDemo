Panopto-UploadAPIDemo
=====================

Panopto Upload API Demo

This program uses Panopto RESTful API and C# to accomplish upload to server.

This program will need AWSSDK to run as it uses Amazon S3 Services.

AWSSDK can be downloaded from here: http://aws.amazon.com/s3/

This program runs on default values if not options are given.

Options for this program is stated below, must have all 6 options if choosing to run with options.

UploadAPIDemo.exe [server] [filePath] [userName] [userPassword] [folderID] [sessionName]

        server: Target server
        filePath: Target upload file path relative to current directory
        userName: User name to access the server
        userPassword: Password for the user
        folderID: ID of the destination folder
        sessionName: Name of the session
