using System;
using Microsoft.Win32;
using NUnit.Framework;

namespace DotNetWindowsRegistry.Tests
{
    public class InMemoryRegistryTests
    {
        [Test]
        public void Can_Print_Simple_Structure()
        {
            //  Create an in-memory registry, some simple structure.
            var registry = new InMemoryRegistry();
            using (var key = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
            {
                var user = key.CreateSubKey("User");
                user.SetValue("Name", "Dave");
                user.SetValue("Number", 34);
                var address = key.CreateSubKey("Address");
                address.SetValue("City", "Singapore");
            }

            var print = registry.Print(RegistryView.Registry64);
            Assert.That(print, Is.EqualTo(string.Join(Environment.NewLine,
                @"HKEY_CURRENT_USER",
                @"  Address",
                @"    City = Singapore",
                @"  User",
                @"    Name = Dave",
                @"    Number = 34")));
        }

        [Test]
        public void Can_Assert_Structure()
        {
            //  Create an in-memory registry, with a given structure.
            var registry = new InMemoryRegistry();
            registry.AddStructure(RegistryView.Registry32,
@"HKEY_CLASSES_ROOT
  .myp
    (Default) = MyProgram.1
  CLSID
    {00000000-1111-2222-3333-444444444444}
      InProcServer32
        (Default) = C:\MyDir\MyCommand.dll
        ThreadingModel = Apartment
    {11111111-2222-3333-4444-555555555555}
      InProcServer32
        (Default) = C:\MyDir\MyPropSheet.dll
        ThreadingModel = Apartment
  MyProgram.1
    (Default) = MyProgram Application
    ShellEx
      ContextMenuHandler
        MyCommand
          (Default) = {00000000-1111-2222-3333-444444444444}
      PropertySheetHandlers
        MyPropSheet
          (Default) = {11111111-2222-3333-4444-555555555555}");

            var print = registry.Print(RegistryView.Registry32);
            Assert.That(print, Is.EqualTo(
@"HKEY_CLASSES_ROOT
  .myp
    (Default) = MyProgram.1
  CLSID
    {00000000-1111-2222-3333-444444444444}
      InProcServer32
        (Default) = C:\MyDir\MyCommand.dll
        ThreadingModel = Apartment
    {11111111-2222-3333-4444-555555555555}
      InProcServer32
        (Default) = C:\MyDir\MyPropSheet.dll
        ThreadingModel = Apartment
  MyProgram.1
    (Default) = MyProgram Application
    ShellEx
      ContextMenuHandler
        MyCommand
          (Default) = {00000000-1111-2222-3333-444444444444}
      PropertySheetHandlers
        MyPropSheet
          (Default) = {11111111-2222-3333-4444-555555555555}"));
        }

        [Test]
        public void Set_Structure_Throws_With_Invalid_Indentation()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                //  Indentation on line two is not a multiple of two.
                new InMemoryRegistry().AddStructure(RegistryView.Registry32, string.Join(Environment.NewLine,
                    @"HKEY_CLASSES_ROOT",
                    @" .myp",
                    @"    (Default) = MyProgram.1"));
            });
            Assert.That(exception.Message,
                Is.EqualTo("Line 2: Invalid indentation. Line starts with 1 spaces. Lines should start with a number of spaces which is a multiple of 2."));
        }

        [Test]
        public void Set_Structure_Throws_With_Invalid_Hive()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                //  HKEY_INVALID_ROOT is not a valid hive name.
                new InMemoryRegistry().AddStructure(RegistryView.Registry32, string.Join(Environment.NewLine,
                    @"HKEY_INVALID_ROOT",
                    @"  .myp",
                    @"    (Default) = MyProgram.1"));
            });
            Assert.That(exception.Message,
                Is.EqualTo("Line 1: HKEY_INVALID_ROOT is not a known registry hive key."));

        }

        [Test]
        public void Set_Structure_Throws_With_Invalid_Depth()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                //  Indentation on line thre is wrong, it is too deep.
                new InMemoryRegistry().AddStructure(RegistryView.Registry32, string.Join(Environment.NewLine,
                    @"HKEY_CLASSES_ROOT",
                    @"  .myp",
                    @"        (Default) = MyProgram.1"));
            });
            Assert.That(exception.Message,
                Is.EqualTo("Line 3: This line is at an invalid depth."));
        }
    }
}
