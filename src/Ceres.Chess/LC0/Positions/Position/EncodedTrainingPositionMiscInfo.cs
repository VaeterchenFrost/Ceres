#region License notice

/*
  This file is part of the Ceres project at https://github.com/dje-dev/ceres.
  Copyright (C) 2020- by David Elliott and the Ceres Authors.

  Ceres is free software under the terms of the GNU General Public License v3.0.
  You should have received a copy of the GNU General Public License
  along with Ceres. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

#region Using directives

using System;
using System.Runtime.InteropServices;

#endregion

namespace Ceres.Chess.EncodedPositions
{
  /// <summary>
  /// Raw data structure in training data containing both miscellaneous info
  /// relating to both position and training result.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public readonly unsafe struct EncodedTrainingPositionMiscInfo : IEquatable<EncodedTrainingPositionMiscInfo>
  {
    /// <summary>
    /// Miscellaneous info relating to position.
    /// </summary>
    public readonly EncodedPositionMiscInfo InfoPosition;

    /// <summary>
    /// Miscellaneous information relating to training data.
    /// </summary>
    public readonly EncodedPositionEvalMiscInfoV6 InfoTraining;


    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="infoPosition"></param>
    /// <param name="infoTraining"></param>
    public EncodedTrainingPositionMiscInfo(EncodedPositionMiscInfo infoPosition, EncodedPositionEvalMiscInfoV6 infoTraining)
    {
      InfoPosition = infoPosition;
      InfoTraining = infoTraining;
    }


    /// <summary>
    /// Sets the miscellaneous flags as specified.
    /// </summary>
    /// <param name="castling_US_OOO"></param>
    /// <param name="castling_US_OO"></param>
    /// <param name="castling_Them_OOO"></param>
    /// <param name="castling_Them_OO"></param>
    /// <param name="blackToMove"></param>
    /// <param name="rule50Count"></param>
    /// <param name="moveCount"></param>
    public void SetMisc(byte castling_US_OOO, byte castling_US_OO, byte castling_Them_OOO, byte castling_Them_OO, byte blackToMove, byte rule50Count)
    {
      fixed (byte* c = &InfoPosition.Castling_US_OOO) *c = castling_US_OOO;
      fixed (byte* c = &InfoPosition.Castling_US_OO) *c = castling_US_OO;
      fixed (byte* c = &InfoPosition.Castling_Them_OOO) *c = castling_Them_OOO;
      fixed (byte* c = &InfoPosition.Castling_Them_OO) *c = castling_Them_OO;

      fixed (EncodedPositionMiscInfo.SideToMoveEnum* c = &InfoPosition.SideToMove) *c = (EncodedPositionMiscInfo.SideToMoveEnum)blackToMove;
      fixed (byte* c = &InfoPosition.Rule50Count) *c = rule50Count;
    }

    public enum GameResultEnum 
    {
      Loss ,
      Draw,
      Win
    }

    public void SetResult(GameResultEnum gameResult)
    {
      fixed (float* ptrQ = &InfoTraining.ResultQ)
      {
        fixed (float* ptrD = &InfoTraining.ResultD)
        {
          switch (gameResult)
          {
            case GameResultEnum.Win:
              *ptrQ = 1.0f;
              *ptrD = 0.0f;
              break;

            case GameResultEnum.Draw:
              *ptrQ = 0.0f;
              *ptrD = 0.0f;
              break;

            case GameResultEnum.Loss:
              *ptrQ = -1.0f;
              *ptrD = 0.0f;
              break;

          }
        }
      }
    }

    public float BestQW => 0.5f * (1.0f - InfoTraining.BestD + InfoTraining.BestQ);
    public float BestQD => InfoTraining.BestD;
    public float BestQL => 0.5f * (1.0f - InfoTraining.BestD - InfoTraining.BestQ);

    public float[] BestQArray => new float[] { BestQW, BestQD, BestQL };

    static EncodedPositionMiscInfo.ResultCode ReversedResult(EncodedPositionMiscInfo.ResultCode result) => result == EncodedPositionMiscInfo.ResultCode.Draw ? EncodedPositionMiscInfo.ResultCode.Draw
                                                                                     : (result == EncodedPositionMiscInfo.ResultCode.Win ? EncodedPositionMiscInfo.ResultCode.Loss : EncodedPositionMiscInfo.ResultCode.Win);
    public EncodedPositionMiscInfo.ResultCode ResultFromWhitePerspective => WhiteToMove ? InfoTraining.ResultFromOurPerspective : ReversedResult(InfoTraining.ResultFromOurPerspective);

    public bool WhiteToMove => InfoPosition.SideToMove == 0;

    #region Overrides

    public override bool Equals(object obj)
    {
      if (obj is EncodedTrainingPositionMiscInfo)
        return Equals((EncodedTrainingPositionMiscInfo)obj);
      else
        return false;
    }


    public bool Equals(EncodedTrainingPositionMiscInfo other)
    {
      return this.InfoPosition.Equals(other.InfoPosition)
          && this.InfoTraining.Equals(other.InfoTraining);

    }

    public override int GetHashCode()
    {
      return HashCode.Combine(InfoPosition.GetHashCode(), InfoTraining.GetHashCode());
    }

    #endregion
  }
}
