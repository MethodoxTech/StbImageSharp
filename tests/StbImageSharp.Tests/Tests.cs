using StbImageSharp.Tests.Utility;
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace StbImageSharp.Tests
{
	public class Tests
	{
		private static readonly Assembly _assembly = typeof(Tests).Assembly;

		[Theory]
		[InlineData("The Public Domain_ Enclosing the Commons of the Mind.pdf")]
		[InlineData("empty")]
		public void LoadUnknownFormat(string filename)
		{
			Assert.Throws<InvalidOperationException>(() =>
			{
				ImageResult result = null;
				using (Stream stream = _assembly.OpenResourceStream(filename))
				{
					result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
				}
			});
		}

		[Theory]
		[InlineData("IDockable.png", 715, 426, ColorComponents.RedGreenBlueAlpha)]
		[InlineData("sample_1280×853.hdr", 1280, 853, ColorComponents.RedGreenBlue)]
		[InlineData("DockPanes.jpg", 609, 406, ColorComponents.RedGreenBlue)]
		public void Load(string filename, int width, int height, ColorComponents colorComponents)
		{
			ImageResult result = null;
			using (Stream stream = _assembly.OpenResourceStream(filename))
			{
				result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
			}

			Assert.NotNull(result);
			Assert.Equal(width, result.Width);
			Assert.Equal(height, result.Height);
			Assert.Equal(ColorComponents.RedGreenBlueAlpha, result.Comp);
			Assert.Equal(colorComponents, result.SourceComp);
			Assert.NotNull(result.Data);
			Assert.Equal(result.Width * result.Height * 4, result.Data.Length);
		}

		[Theory]
		[InlineData("sample_1280×853.hdr", 1280, 853, ColorComponents.RedGreenBlue)]
		public void LoadHdr(string filename, int width, int height, ColorComponents colorComponents)
		{
			ImageResultFloat result = null;
			using(Stream stream = _assembly.OpenResourceStream(filename))
			{
				result = ImageResultFloat.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
			}

			Assert.NotNull(result);
			Assert.Equal(width, result.Width);
			Assert.Equal(height, result.Height);
			Assert.Equal(ColorComponents.RedGreenBlueAlpha, result.Comp);
			Assert.Equal(colorComponents, result.SourceComp);
			Assert.NotNull(result.Data);
			Assert.Equal(result.Width * result.Height * 4, result.Data.Length);
		}

		[Theory]
		[InlineData("sample_1280×853.hdr", 2000, 1280, 853, ColorComponents.RedGreenBlue, false)]
		[InlineData("DockPanes.jpg", 2000, 609, 406, ColorComponents.RedGreenBlue, false)]
		public void Info(string filename, int headerSize, int width, int height, ColorComponents colorComponents, bool is16bit)
		{
			ImageInfo? result;

            byte[] data = new byte[headerSize];
            using (Stream stream = _assembly.OpenResourceStream(filename))
            {
                stream.Read(data, 0, data.Length);
            }

            using (MemoryStream stream = new(data))
            {
                result = ImageInfo.FromStream(stream);
            }

			Assert.NotNull(result);

			var info = result.Value;
			Assert.Equal(width, info.Width);
			Assert.Equal(height, info.Height);
			Assert.Equal(colorComponents, info.ColorComponents);
			Assert.Equal(is16bit ? 16 : 8, info.BitsPerChannel);
		}

		[Theory]
		[InlineData("somersault.gif", 384, 480, ColorComponents.RedGreenBlueAlpha, 43)]
		public void AnimatedGifFrames(string fileName, int width, int height, ColorComponents colorComponents, int originalFrameCount)
		{
			using (Stream stream = _assembly.OpenResourceStream(fileName))
			{
				var frameCount = 0;
				foreach(AnimatedFrameResult frame in ImageResult.AnimatedGifFramesFromStream(stream))
				{
					Assert.Equal(width, frame.Width);
					Assert.Equal(height, frame.Height);
					Assert.Equal(colorComponents, frame.Comp);
					Assert.NotNull(frame.Data);
					Assert.Equal(frame.Width * frame.Height * (int)frame.Comp, frame.Data.Length);

                    ++frameCount;
                }

				Assert.Equal(frameCount, originalFrameCount);

                stream.Seek(0, SeekOrigin.Begin);
            }

			Assert.Equal(0, StbImage.NativeAllocations);
		}
	}
}
