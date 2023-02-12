﻿using HarmonyLib;
using m2d;
using nel;
using PixelLiner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XX;

namespace BobOnGradle
{
	public class Patches
	{
		public static bool passBurst = false;
		[HarmonyPatch(typeof(PR),"initDeath")]
		[HarmonyPrefix]
		public static bool initDeath()
		{
			if (Utils.GetNoel().getEH((EnhancerManager.EH)(1 << Plugin.idHutao)))
			{
				PR pr = Utils.GetNoel();
				passBurst = true;
				pr.addNoDamage(m2d.NDMG._BURST_PREVENT, 1f);
				Plugin.instance.scheduleNextTick(() =>
				{
					pr.changeState(PR.STATE.BURST);
					pr.cureHp(99);
					pr.Skill.BurstSel.fineFaintedRatio();
				});
				pr.Ser.CureAll();
				return false;
			}
			return true;
		}
		[HarmonyPatch(typeof(MagicData),"calcBurstFaintedRatio")]
		[HarmonyPrefix]
		public static bool calcBurstFaintedRatio(ref float __result)
		{
			if(passBurst)
			{
				__result = 0;
				return false;
			}
			return true;
		}
		[HarmonyPatch(typeof(PR),"applyHpDamageRatio")]
		[HarmonyPostfix]
		public static void applyHpDamage(PR __instance,ref float __result)
		{
			if(__instance.getEH((EnhancerManager.EH)(1<<Plugin.idNoelle)))
				__result *= (1f-0.5f*__instance.mp_ratio);
		}
		[HarmonyPatch(typeof(NelEnemy),"applyHpDamageRatio")]
		[HarmonyPostfix]
		public static void applyEnemyDamage(AttackInfo Atk,ref float __result)
		{
			if(Atk is NelAttackInfo)
			{
				NelAttackInfo info = Atk as NelAttackInfo;
				if(info.Caster is PR)
				{
					PR pr = info.Caster as PR;
					if (pr.getEH((EnhancerManager.EH)(1 << Plugin.idNoelle)))
						__result *= (1f + 0.25f * pr.mp_ratio);
				}
			}
		}
		[HarmonyPatch(typeof(NelEnemy),"changeStateToDie")]
		[HarmonyPrefix]
		public static void onEnemyDeath(NelEnemy __instance)
		{
			NelAttackInfo info=__instance.DeathAtk;
			if(info != null)
			{
				if(info.Caster is PR)
				{
					PR pr = info.Caster as PR;
					int got=224;
					if (pr.getEH((EnhancerManager.EH)(1 << Plugin.idNoelle)))
						pr.Skill.getOverChargeSlots().getMana(224,ref got);
				}
			}
		}
		public static Dictionary<NelEnemy, MeshDrawer> draw = new();
		public static Material mat=null;
		[HarmonyPatch(typeof(EnemyAnimator),"FnEnRenderBaseInner")]
		[HarmonyPostfix]
		public static void animate(
			ref int draw_id,ref M2RenderTicket Tk, ref MeshDrawer MdOut,ref bool __result,
			NelEnemy ___Mv,Map2d ___Mp,M2RenderTicket ___RtkBuf)
		{
			//Console.WriteLine($"draw {draw_id}");
			if (draw_id == 5)
				__result = true;
			if(draw_id!=6)
			{
				return;
			}
			if (___Mv.is_alive)
			{
				M2MoverPr aimPr = ___Mv.AimPr;
				if (aimPr != null && aimPr.is_alive)
				{
					if (draw.ContainsKey(___Mv))
						MdOut = draw.GetValueSafe(___Mv);
					else
						draw[___Mv]=MdOut = new MeshDrawer();
					MdOut.clear();
					Console.WriteLine("sz " + ___Mv.sizey);
					Tk.Matrix = ___RtkBuf.Matrix;
					PxlCharacter chara = Plugin.nahida;
					PxlPose pose = chara.getPoseByName("trikarma");
					PxlSequence seq = pose.getSequence(0);
					PxlFrame frame = seq.getFrame(1);
					if(mat==null)
						mat = MTRX.blend2Mtr(BLEND.NORMAL, frame);
					MdOut.activate("trikarma", mat, false, new Color32(255,255,255,255));
					MdOut.initForImg(frame.getLayer(0).Img);
					MdOut.Rect(0, 0, 32, 32);
					__result = true;
				}
			}
		}
		[HarmonyPatch(typeof(PR), "applyBurstMpDamage")]
		[HarmonyPrefix]
		public static bool applyBurstMpDamage()
		{
			if(passBurst)
			{
				passBurst = false;
				return false;
			}
			return true;
		}
	}
}
