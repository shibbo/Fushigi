using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenGL;

namespace Fushigi.gl
{
    public partial class GLFramebuffer : GLObject
    {
        public FramebufferTarget Target { get; }

        public InternalFormat PixelInternalFormat { get; internal set; }

        public uint Width { get; internal set; }

        public uint Height { get; internal set; }

        public List<IFramebufferAttachment> Attachments { get; }

        private GL _gl;

        public GLFramebuffer(GL gl, FramebufferTarget framebufferTarget) : base(gl.GenFramebuffer()) {
            _gl = gl;
            Target = framebufferTarget;
            Attachments = new List<IFramebufferAttachment>();
        }

        public GLFramebuffer(GL gl, FramebufferTarget target, uint width, uint height,
          InternalFormat pixelInternalFormat = InternalFormat.Rgba, int colorAttachmentsCount = 1, bool useDepth = true)
          : this(gl, target)
        {
            if (colorAttachmentsCount < 0)
                throw new ArgumentOutOfRangeException(nameof(colorAttachmentsCount), "Color attachment count must be non negative.");

            Bind();
            PixelInternalFormat = pixelInternalFormat;
            Width = width;
            Height = height;

            if (colorAttachmentsCount > 0)
                Attachments = CreateColorAttachments(width, height, colorAttachmentsCount);

            if (useDepth)
                SetUpRboDepth(width, height);
        }

        public GLFramebuffer(GL gl, FramebufferTarget target, uint width, uint height, uint numSamples,
            InternalFormat pixelInternalFormat = InternalFormat.Rgba, int colorAttachmentsCount = 1)
     : this(gl, target)
        {
            if (colorAttachmentsCount < 0)
                throw new ArgumentOutOfRangeException(nameof(colorAttachmentsCount), "Color attachment count must be non negative.");

            Bind();
            PixelInternalFormat = pixelInternalFormat;
            Width = width;
            Height = height;

            Attachments = CreateColorAttachments(width, height, colorAttachmentsCount, numSamples);

            SetUpRboDepth(width, height, numSamples);
        }

        public void Bind() {
            _gl.BindFramebuffer(Target, ID);
        }

        public void Unbind() {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Dispose() {
            _gl.DeleteFramebuffer(ID);
        }

        public void Resize(uint width, uint height)
        {
            Width = width;
            Height = height;
            foreach (var attatchment in Attachments)
            {
                if (attatchment is GLTexture2D)
                {
                    var tex = (GLTexture2D)attatchment;
                    tex.Resize(Width, Height);
                }
                else if(attatchment is Renderbuffer)
                {
                    var buffer = (Renderbuffer)attatchment;
                    buffer.Bind();
                    _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, buffer.InternalFormat, Width, Height);
                    buffer.Unbind();
                }
            }
        }

        public void DisposeRenderBuffer()
        {
            foreach (var attatchment in Attachments)
                if (attatchment is Renderbuffer)
                    attatchment.Dispose();
        }

        public void SetDrawBuffers(params DrawBufferMode[] drawBuffers)
        {
            Bind();
            _gl.DrawBuffers((uint)drawBuffers.Length, drawBuffers);
        }

        public void SetReadBuffer(ReadBufferMode readBufferMode)
        {
            Bind();
            _gl.ReadBuffer(readBufferMode);
        }

        public FramebufferStatus GetStatus()
        {
            Bind();
            return (FramebufferStatus)_gl.CheckFramebufferStatus(Target);
        }

        public void AddAttachment(FramebufferAttachment attachmentPoint, IFramebufferAttachment attachment)
        {
            // Check if the dimensions are uninitialized.
            if (Attachments.Count == 0 && Width == 0 && Height == 0)
            {
                Width = attachment.Width;
                Height = attachment.Height;
            }

            if (attachment.Width != Width || attachment.Height != Height)
                throw new ArgumentOutOfRangeException(nameof(attachment), "The attachment dimensions do not match the framebuffer's dimensions.");

            attachment.Attach(attachmentPoint, this);
            Attachments.Add(attachment);
        }

        private GLTexture2D CreateColorAttachment(uint width, uint height)
        {
            GLTexture2D texture = GLTexture2D.CreateUncompressedTexture(_gl, width, height,
                                        PixelInternalFormat, PixelFormat.Rgba, PixelType.UnsignedByte);
            // Don't use mipmaps for color attachments.
            texture.MinFilter = TextureMinFilter.Linear;
            texture.MagFilter = TextureMagFilter.Linear;
            texture.Bind();
            texture.UpdateParameters();
            texture.Unbind();

            return texture;
        }

        private GLTexture2D CreateColorAttachment(uint width, uint height, uint numSamples)
        {
            //TODO multi sample texture
            GLTexture2D texture = GLTexture2D.CreateUncompressedTexture(_gl, width, height,
                                        PixelInternalFormat, PixelFormat.Rgba, PixelType.UnsignedByte);
            // Don't use mipmaps for color attachments.
            texture.MinFilter = TextureMinFilter.Linear;
            texture.MagFilter = TextureMagFilter.Linear;
            texture.Bind();
            texture.UpdateParameters();
            texture.Unbind();

            return texture;
        }

        private void SetUpRboDepth(uint width, uint height)
        {
            // Render buffer for the depth attachment, which is necessary for depth testing.
            Renderbuffer rboDepth = new Renderbuffer(_gl, width, height, InternalFormat.Depth24Stencil8);
            AddAttachment(FramebufferAttachment.DepthStencilAttachment, rboDepth);
        }

        private void SetUpRboDepth(uint width, uint height, uint numSamples)
        {
            // Render buffer for the depth attachment, which is necessary for depth testing.
            Renderbuffer rboDepth = new Renderbuffer(_gl, width, height, numSamples, InternalFormat.Depth24Stencil8);
            AddAttachment(FramebufferAttachment.DepthStencilAttachment, rboDepth);
        }

        private List<IFramebufferAttachment> CreateColorAttachments(uint width, uint height, int colorAttachmentsCount)
        {
            var colorAttachments = new List<IFramebufferAttachment>();

            List<DrawBufferMode> attachmentEnums = new List<DrawBufferMode>();
            for (int i = 0; i < colorAttachmentsCount; i++)
            {
                DrawBufferMode attachmentPoint = DrawBufferMode.ColorAttachment0 + i;
                attachmentEnums.Add(attachmentPoint);

                GLTexture2D texture = CreateColorAttachment(width, height);
                colorAttachments.Add(texture);
                AddAttachment((FramebufferAttachment)attachmentPoint, texture);
            }

            SetDrawBuffers(attachmentEnums.ToArray());

            return colorAttachments;
        }

        private List<IFramebufferAttachment> CreateColorAttachments(uint width, uint height, int colorAttachmentsCount, uint numSamples)
        {
            var colorAttachments = new List<IFramebufferAttachment>();

            List<DrawBufferMode> attachmentEnums = new List<DrawBufferMode>();
            for (int i = 0; i < colorAttachmentsCount; i++)
            {
                DrawBufferMode attachmentPoint = DrawBufferMode.ColorAttachment0 + i;
                attachmentEnums.Add(attachmentPoint);

                GLTexture2D texture = CreateColorAttachment(width, height, numSamples);
                colorAttachments.Add(texture);
                AddAttachment((FramebufferAttachment)attachmentPoint, texture);
            }

            SetDrawBuffers(attachmentEnums.ToArray());

            return colorAttachments;
        }
    }
}
