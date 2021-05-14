/* PlayStation(R)Mobile SDK 2.00.00
 * Copyright (C) 2014 Sony Computer Entertainment Inc.
 * All Rights Reserved.
 */
using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using Sce.PlayStation.Core;
using Sce.PlayStation.Core.Graphics;
using Sce.PlayStation.Core.Imaging;
using Sce.PlayStation.Core.Audio;
using Sce.PlayStation.Core.Environment;
using Sample;

namespace BadApplePSM
{

/**
 * ImageSample
 */
public static class AppMain
{
    private static GraphicsContext graphics;
    private static Bgm bgm;
    private static BgmPlayer bgmPlayer;
    static bool loop = true;
	static Stopwatch timer;
	static FileStream decodeStream;
	static bool lastColor = true; // True = black, False = white
	static Texture2D tex;
	static SampleSprite spr;
	static Image FrameImg;
	static ImageColor black = new ImageColor(0,0,0,255);
	static ImageColor white = new ImageColor(255,255,255,255);
	static uint totalWhite = 0;
	static uint totalBlack = 0;

	public static void CopyFile(string src, string dst)
	{
		byte[] srcBytes = File.ReadAllBytes(src);
		File.WriteAllBytes(dst, srcBytes);
	}
    public static void Main(string[] args)
    {
			
        Init();
        while (loop) {
            SystemEvents.CheckEvents();
            Update();
            Render();
        }

        Term();
    }
		
	public static int frameCount = 0;
	public static long limit = 33;
	public static long lastTime = 0;
	
	public static void Copy(string src, string dst)
	{
		FileStream fs1 = File.OpenRead(src);
		FileStream fs2 = File.OpenWrite(dst);
		fs1.CopyTo(fs2);
		fs1.Close();
		fs2.Close();
	}
    public static bool Init()
    {
		decodeStream = File.OpenRead("/Application/apple.psmvideo");
			
        graphics = new GraphicsContext();
        SampleDraw.Init(graphics);
		bgm = new Bgm("/Application/apple.mp3");
        bgmPlayer = bgm.CreatePlayer();
		FrameImg = new Image(ImageMode.Rgba, new ImageSize(480, 360), new ImageColor(0,0,0,255));
		
		tex = createTexture(FrameImg);	
		spr = new SampleSprite(tex, ((960 / 2) - 480), ((544 / 2) - 360));
		
		timer = new Stopwatch();			
		bgmPlayer.Play();
		
		
        return true;
    }
	public static void NextFrame()
	{
		frameCount++;
		uint runLength = 0x00;
		byte[] uBytes = new byte[4];
		
		int x = 0;
		int y = 0;			
		totalBlack = 0;
		totalWhite = 0;
		while(true)
		{
			decodeStream.Read(uBytes, 0x00, 3);
			runLength = BitConverter.ToUInt32(uBytes,0);
			
			if(runLength == 0xFFFFFF)
			{
				lastColor = true;
				break;
			}
			if(runLength == 0xFEFEFE)
			{
				lastColor = false;
				break;
			}	
			if(lastColor)
				totalBlack += runLength;
			else
				totalWhite += runLength;

			uint totalLength = 0;
			while(totalLength < runLength){
				int thisRow = 0;
				int ax=x;
				int ay=y;
				if((x + runLength)-totalLength < 480)
				{
					thisRow = (int)(runLength-totalLength);
					ax += thisRow;
				}
				else
				{
					thisRow = 480 - x;
					ay += 1;
					ax = 0;
				}
				
				ImageRect loc = new ImageRect((int)x, (int)y, (int)thisRow, 1);
				FrameImg.DrawRectangle(lastColor ? black : white, loc);
				x = ax;
				y = ay;
				totalLength += (uint)thisRow;
			}
			
			lastColor = !lastColor;
			
		}			
		tex = createTexture(FrameImg);	
		spr = new SampleSprite(tex, ((960 / 2) - 480/2), ((544 / 2) - 360/2));
	}
		
	/// Image -> Texture2D conversion
    private static Texture2D createTexture(Image image)
    {
        var texture = new Texture2D(image.Size.Width, image.Size.Height, false, PixelFormat.Rgba);
        texture.SetPixels(0, image.ToBuffer());

        return texture;
    }

    /// Terminate
    public static void Term()
    {
        SampleDraw.Term();
        graphics.Dispose();
    }

    public static bool Update()
    {
		timer.Reset();		
		timer.Start();
		NextFrame();
		

		while(bgmPlayer.Time <= 0.001){};
		
		if(frameCount == 6571)
			loop = false;
		
        SampleDraw.Update();
		
		return true;
    }
    public static bool Render()
    {
		bool clearColor = (totalBlack > totalWhite);
		graphics.SetViewport(0, 0, graphics.GetFrameBuffer().Width, graphics.GetFrameBuffer().Height);
		if(clearColor)
			graphics.SetClearColor(0.0f, 0.0f, 0.0f, 255.0f);
		else
			graphics.SetClearColor(255.0f, 255.0f, 255.0f, 255.0f);
        graphics.Clear();

		SampleDraw.DrawText("Bad Apple!!! Mobile", clearColor ? 0xFFFFFFFF : 0xFF000000, 0, 0);
		SampleDraw.DrawText("Frame "+frameCount.ToString()+"/6572", clearColor ? 0xFFFFFFFF : 0xFF000000, 0, 510);
		SampleDraw.DrawSprite(spr);
			
		graphics.SwapBuffers();
		
		while (timer.ElapsedMilliseconds < (limit - lastTime)) { };
		lastTime = timer.ElapsedMilliseconds - (limit - lastTime);
		
		return true;
    }
}
	
} // Sample
