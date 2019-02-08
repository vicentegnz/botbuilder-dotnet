namespace Microsoft.Bot.Builder.Integration.AspNet.Core
{
    internal class NamedPipeConnectionInterrupt
    {
        public virtual void BeforeWriteLength() { }

        public virtual void BeforeWriteContent() { }

        public virtual void BeforeWriteEnd() { }

        public virtual void BeforeReadLength() { }

        public virtual void BeforeReadContent() { }

        public virtual void BeforeReadEnd() { }
    }
}
