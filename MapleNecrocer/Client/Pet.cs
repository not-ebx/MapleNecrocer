﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using WzComparerR2.WzLib;
using System.Drawing;
using Spine;

namespace MapleNecrocer;

public class Pet : JumperSprite
{
    public Pet(Sprite Parent) : base(Parent)
    {
        SpriteSheetMode = SpriteSheetMode.NoneSingle;
    }

    int FTime;
    Foothold WallFH;
    Foothold BelowFH;
    MoveDirection MoveDirection;
    float MoveSpeed;
    string PetName;
    int NameWidth;
    int IDWidth;
    MoveType MoveType;
    int FallEdge;
    int JumpEdge;
    Wz_Vector origin = new(0, 0);
    string Path;
    string UpPath;
    string State;
    int Frame;
    int Delay;
    Vector2 Distance;
    bool OnLadder;
    Foothold FH;
    public static Pet Instance;

    public static void Create(string ID)
    {
        Wz_Node Entry = Wz.GetNode("Item/Pet/" + ID + ".img");
        Wz.DumpData(Entry, Wz.EquipData, Wz.EquipImageLib);
        foreach (var Iter in Wz.EquipData[Entry.FullPathToFile2()].Nodes)
        {
            foreach (var Iter2 in Wz.EquipData[Iter.FullPathToFile2()].Nodes)
            {
                if (Char.IsNumber(Iter2.Text[0]))
                {
                    if (Iter.Text == "stand0" && Iter2.Text == "0")
                    {
                        var Pet = new Pet(EngineFunc.SpriteEngine);
                        Pet.ImageLib = Wz.EquipImageLib;
                        Pet.IntMove = true;
                        Pet.Tag = 1;
                        Pet.State = Iter.Text;
                        Pet.Frame = Iter2.Text.ToInt();
                        Pet.UpPath = Entry.FullPathToFile2();
                        Pet.ImageNode = Wz.EquipData[Iter2.FullPathToFile2()];
                        var StartX = Game.Player.X - 60;
                        if (StartX < Map.Left)
                            StartX = Game.Player.X;
                        var Pos = FootholdTree.Instance.FindBelow(new Vector2(StartX, Game.Player.Y - 3), ref Pet.BelowFH);
                        Pet.MoveType = MoveType.Jump;
                        Pet.X = Pos.X;
                        Pet.Y = Pos.Y;
                        Pet.FH = Pet.BelowFH;
                        Pet.Z = Game.Player.Z;
                        Pet.JumpSpeed = 0.6f;
                        Pet.JumpHeight = 9;
                        Pet.MaxFallSpeed = 8;
                        Pet.MoveDirection = MoveDirection.None;
                        Pet.MoveSpeed = 2.5f;
                    }
                }
            }
        }
    }


    public override void DoMove(float Delta)
    {
        base.DoMove(Delta);
        int X1 = FH.X1;
        int Y1 = FH.Y1;
        int X2 = FH.X2;
        int Y2 = FH.Y2;

        if (Wz.HasDataE(UpPath + "/" + State + "/" + Frame))
        {
            Path = UpPath + "/" + State + "/" + Frame;
            ImageNode = Wz.EquipData[Path];
        }

        if (Wz.HasDataE(UpPath + "/" + State + "/" + Frame + "/delay"))
            Delay = Wz.EquipData[UpPath + "/" + State + "/" + Frame + "/delay"].ToInt();
        else
            Delay = 100;

        FTime += 17;
        if (FTime > Delay)
        {
            Frame += 1;
            if (!Wz.EquipData.ContainsKey(UpPath + "/" + State + "/" + Frame))
                Frame = 0;
            FTime = 0;
        }

        Distance.X = Math.Abs(Game.Player.X - X);
        Distance.Y = Math.Abs(Game.Player.Y - Y);

        if (Distance.X > 70)
        {
            State = "move";
            if (Game.Player.X > X)
            {
                FlipX = true;
                MoveDirection = MoveDirection.Right;
            }
            if (Game.Player.X < X)
            {
                FlipX = false;
                MoveDirection = MoveDirection.Left;
            }
        }
        else
        {
            State = "stand0";
            MoveDirection = MoveDirection.None;
        }

        if (Game.Player.Y < Y)
        {
            switch (Distance.Y)
            {
                case float i when i >= 100 && i <= 150:
                    if (JumpState == JumpState.jsNone)
                    {
                        //  Below := TFootholdTree.This.FindBelow(Point(Round(X), Round(Y - 70)), BelowFH);
                        //  if Y - Below.Y <> 0 then
                        DoJump = true;
                    }
                    break;

                case float i when i >= 151 && i <= 2000:
                    if (Game.Player.JumpState == JumpState.jsNone)
                    {
                        X = Game.Player.X;
                        Y = Game.Player.Y;
                    }
                    break;
            }
        }

        if (Game.Player.Y > Y)
        {
            if (Distance.Y >= 200 && Distance.Y <= 200)
            {

                Y += 5;
                JumpState = JumpState.jsFalling;
            }
        }

        Vector2 Below;
        if (Game.Player.InLadder)
        {
            LadderRope ladderRope = LadderRope.Find(new Vector2(Game.Player.X, Game.Player.Y), ref OnLadder);
            State = "hang";
            X = Game.Player.X;
            Y = Game.Player.Y + 20;
            Z = 7 * 100000 + 60000;
            if (Y > ladderRope.Y2 - 10)
                JumpState = JumpState.jsFalling;
            if (Y < ladderRope.Y1 + 30)
            {
                Below = FootholdTree.Instance.FindBelow(new Vector2(Game.Player.X, Game.Player.Y - 100), ref BelowFH);
                Y = Below.Y;
                FH = BelowFH;
            }
        }

        if (JumpState == JumpState.jsFalling)
        {
            Below = FootholdTree.Instance.FindBelow(new Vector2(X, Y - VelocityY - 2), ref BelowFH);
            if (Y >= Below.Y - 3)
            {
                Y = Below.Y;
                // MaxFallSpeed :=10;
                JumpState = JumpState.jsNone;
                FH = BelowFH;
                Z = FH.Z * 100000 + 70000;
            };
        }

        int FallEdge;
        int Direction;
        if (MoveDirection == MoveDirection.Left)
        {
            Direction = GetAngle256(X2, Y2, X1, Y1);
            if (!FH.IsWall())
            {
                X += (float)(Sin256(Direction) * MoveSpeed);
                Y -= (float)(Cos256(Direction) * MoveSpeed);
            }
            FallEdge = -999999;
            JumpEdge = -999999;
            if (MoveType == MoveType.Move)
            {
                // no fh
                if (FH.Prev == null)
                    FallEdge = FH.X1;
                // Wall's edge down
                if ((FH.Prev != null) && (FH.Prev.IsWall()))
                    FallEdge = FH.X1;

                if (X < FallEdge)
                {
                    X = FallEdge;
                    FlipX = true;
                    MoveDirection = MoveDirection.Right;
                }
            }

            if (MoveType == MoveType.Jump)
            {
                if (X < Map.Left + 20)
                {
                    X = Map.Left + 20;
                    FlipX = true;
                    MoveDirection = MoveDirection.Right;
                }
                // .--------.
                if (FH.Prev == null)
                    JumpEdge = FH.X1;
                // ┌--- <--
                if ((FH.Prev != null) && (FH.Prev.IsWall()) && (FH.Prev.Y1 > Y))
                    FallEdge = FH.X1;

                if (X < FallEdge)
                {
                    if (Game.Player.Y <= Y)
                        DoJump = true;
                    if (Game.Player.Y > Y && JumpState == JumpState.jsNone)
                        JumpState = JumpState.jsFalling;
                }
                if (X < JumpEdge)
                    DoJump = true;
                // -->  ---┐  <--
                WallFH = FootholdTree.Instance.FindWallR(new Vector2(X + 4, Y - 4));
                if ((WallFH != null) && (FH.Z == WallFH.Z))
                {
                    if (X < WallFH.X1 + 30 && Game.Player.Y <= Y)
                        DoJump = true;
                    if (X <= WallFH.X1)
                    {
                        X = WallFH.X1 + MoveSpeed;
                        if (JumpState == JumpState.jsNone)
                        {
                            FlipX = true;
                            MoveDirection = MoveDirection.Right;
                        }
                    }
                }
            }
            // walk left
            if ((X <= FH.X1) && (FH.PrevID != 0) && (!FH.IsWall()) && (!FH.Prev.IsWall()))
            {
                if (JumpState == JumpState.jsNone)
                {
                    FH = FH.Prev;
                    X = FH.X2;
                    Y = FH.Y2;
                    Z = FH.Z * 100000 + 6000;
                }
            }
        }

        // walk right
        if (MoveDirection == MoveDirection.Right)
        {

            Direction = GetAngle256(X1, Y1, X2, Y2);
            if (!FH.IsWall())
            {
                X += (float)(Sin256(Direction) * MoveSpeed);
                Y -= (float)(Cos256(Direction) * MoveSpeed);
            }

            FallEdge = 999999;
            JumpEdge = 999999;
            if (MoveType == MoveType.Move)
            {
                if (FH.Next == null)
                    FallEdge = FH.X2 + 5;
                // Wall down
                if ((FH.Next != null) && (FH.Next.IsWall()))

                    FallEdge = FH.X2;
                if (X > FallEdge)
                {
                    X = FallEdge;
                    FlipX = false;
                    MoveDirection = MoveDirection.Left;
                }
            }

            if (MoveType == MoveType.Jump)
            {
                if (X > Map.Right - 20)
                {
                    X = Map.Right - 20;
                    FlipX = false;
                    MoveDirection = MoveDirection.Left;
                }
                if (FH.Next == null) // .--------.
                    JumpEdge = FH.X2;
                // -->  ----┐
                if ((FH.Next != null) && (FH.Next.IsWall()) && (FH.Next.Y2 > Y))
                    FallEdge = FH.X2;

                if (X > FallEdge)
                {
                    if (Game.Player.Y <= Y)
                        DoJump = true;
                    if (Game.Player.Y > Y && JumpState == JumpState.jsNone)
                        JumpState = JumpState.jsFalling;
                }
                if (X > JumpEdge)
                    DoJump = true;
                // -->  ┌.....
                WallFH = FootholdTree.Instance.FindWallL(new Vector2(X - 4, Y - 4));
                if ((WallFH != null) && (FH.Z == WallFH.Z))
                {
                    if (X > WallFH.X1 - 30 && Game.Player.Y <= Y)
                        DoJump = true;
                    if (X >= WallFH.X1)
                    {
                        X = WallFH.X2 - MoveSpeed;
                        if (JumpState == JumpState.jsNone)
                        {
                            FlipX = false;
                            MoveDirection = MoveDirection.Left;
                        }
                    }
                }
            }

            // walk right
            if ((X >= FH.X2) && (FH.NextID != 0) && (!FH.IsWall()) && (!FH.Next.IsWall()))
            {
                if (JumpState == JumpState.jsNone)
                {
                    FH = FH.Next;
                    X = FH.X1;
                    Y = FH.Y1;
                    Z = FH.Z * 100000 + 6000;
                }
            }
        }

        //if (MoveDirection ==  MoveDirection.None)
        // X = (X);

        if (ImageNode.GetNode("origin") != null)
            origin = ImageNode.GetNode("origin").ToVector();
        switch (FlipX)
        {
            case true:
                Offset.X = origin.X - ImageWidth;
                break;
            case false:
                Offset.X = -origin.X;
                break;
        }
        Offset.Y = -origin.Y;

    }
}

