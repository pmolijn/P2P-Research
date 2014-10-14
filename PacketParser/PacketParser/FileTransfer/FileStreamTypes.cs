namespace PacketParser.FileTransfer
{
    using System;

    public enum FileStreamTypes
    {
        FTP,
        HttpGetChunked,
        HttpGetNormal,
        HttpPost,
        HttpPostMimeMultipartFormData,
        HttpPostMimeFileData,
        OscarFileTransfer,
        SMB,
        SMTP,
        TFTP,
        TlsCertificate
    }
}

