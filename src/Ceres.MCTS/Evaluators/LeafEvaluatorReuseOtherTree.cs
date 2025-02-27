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
using System.Diagnostics;
using Ceres.Chess.EncodedPositions;
using Ceres.MCTS.Iteration;
using Ceres.MCTS.LeafExpansion;
using Ceres.MCTS.MTCSNodes;
using Ceres.MCTS.MTCSNodes.Struct;
using Google.Protobuf.Reflection;

#endregion

namespace Ceres.MCTS.Evaluators
{
  /// <summary>
  /// Leaf evaluator that seeks to reuse position evaluators
  /// indexed by an transposition roots dictionary residing in another subtree.
  /// 
  /// Obviously this is only possible when the other subtree contains compatible evaluations
  /// (such as same neural network).
  /// </summary>
  public class LeafEvaluatorReuseOtherTree : LeafEvaluatorBase
  {
    MCTSIterator OtherContext;

    bool haveVerifiedCompatible = false;

    /// <summary>
    /// Number of successful probes.
    /// </summary>
    public static long NumHits = 0;

    /// <summary>
    /// Number of unsuccessful probes
    /// </summary>
    public static long NumMisses = 0;

    /// <summary>
    /// Fraction of probes which have been successful.
    /// </summary>
    public static float HitRate => (float)NumHits / (float)(NumHits + NumMisses);


    /// <summary>
    /// Constructor (with provided refernece to another MCTSIterator with which to share).
    /// </summary>
    /// <param name="otherContext"></param>
    public LeafEvaluatorReuseOtherTree(MCTSIterator otherContext)
    {
      OtherContext = otherContext;
    }

    protected override LeafEvaluationResult DoTryEvaluate(MCTSNode node)
    {
      VerifyCompatibleNetworkDefinition(node);

      if (OtherContext.Tree.TranspositionRoots != null && 
          OtherContext.Tree.TranspositionRoots.TryGetValue(node.Ref.ZobristHash, out int nodeIndex))
      {
        using (new SearchContextExecutionBlock(OtherContext))
        {
          ref MCTSNodeStruct otherNodeRef = ref OtherContext.Tree.Store.Nodes.nodes[nodeIndex];
          CompressedPolicyVector[] cpvArray = new CompressedPolicyVector[1];

          if (otherNodeRef.Terminal != Chess.GameResult.Unknown)
          {
            NumMisses++;
            return default;
          }

          LeafEvaluationResult ret = new(otherNodeRef.Terminal, otherNodeRef.WinP, otherNodeRef.LossP, otherNodeRef.MPosition, cpvArray, 0);
          MCTSNodeStructUtils.ExtractPolicyVector(OtherContext.ParamsSelect.PolicySoftmax, in otherNodeRef, ref cpvArray[0]);

          NumHits++;
          return ret;
        }
      }
      else
      {
        NumMisses++;
        return default;
      }
    }

    /// <summary>
    /// Returns if the contexts associated with two MCTSIterators are compatiable for reuse,
    /// i.e. the underlying network evaluators will generate identical values for all output heads.
    /// </summary>
    /// <param name="contextCopyFrom"></param>
    /// <param name="contextCopyTo"></param>
    /// <returns></returns>
    public static bool ContextsCompatibleForReuse(MCTSIterator contextCopyFrom, MCTSIterator contextCopyTo)
    {
      if (!contextCopyFrom.EvaluatorDef.NetEvaluationsIdentical(contextCopyTo.EvaluatorDef)) return false;

      return true;
    }


    /// <summary>
    /// Throws an Exception unless the two contexts have compatible network definitions.
    /// </summary>
    /// <param name="node"></param>
    private void VerifyCompatibleNetworkDefinition(MCTSNode node)
    {
      if (!haveVerifiedCompatible)
      {
        if (!ContextsCompatibleForReuse(node.Context, OtherContext))
          throw new Exception("Cannot reuse from other subtree with different parameters (such as different neural network or parameters such as MIN_POLICY_PROBABILITY)");
        haveVerifiedCompatible = true;
      }
    }
  }
}



