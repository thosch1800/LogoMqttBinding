using System;
using Sharp7;
using Xunit;

namespace LogoMqttBindingTests.Infrastructure
{
  public class LogoHardwareMock : ICollectionFixture<LogoHardwareMock>, IDisposable
  {
    public LogoHardwareMock()
    {
      server.RegisterArea(S7Server.srvAreaDB, 1, db, db.Length);
      var errorCode = server.Start();
      if (errorCode != 0) throw new InvalidOperationException($"server start returned {errorCode}: {server.ErrorText(errorCode)}");
    }

    public void Dispose() => server.Stop();

    public int ClientsCount => server.ClientsCount;

    public void WriteBit(int address, int bit, bool value) => db.SetBitAt(address, bit, value);
    public bool ReadBit(int address, int bit) => db.GetBitAt(address, bit);

    public void WriteByte(int address, byte value) => db.SetByteAt(address, value);
    public byte ReadByte(int address) => db.GetByteAt(address);

    public void WriteFloat(int address, float value) => db.SetRealAt(address, value);
    public float ReadFloat(int address) => db.GetRealAt(address);

    public void WriteInteger(int address, short value) => db.SetIntAt(address, value);
    public int ReadInteger(int address) => (short) db.GetIntAt(address);


    private readonly S7Server server = new S7Server();
    private readonly byte[] db = new byte[850]; // maximum local variable memory range is 0..850
  }
}