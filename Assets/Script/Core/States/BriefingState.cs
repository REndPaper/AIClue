using UnityEngine;







public class BriefingState : ISceneChangeState



{



    public TransitionType Transition => TransitionType.SceneChange;



    public string TargetSceneName => "Briefing";







    public void Enter()



    {



        Debug.Log("[State] Briefing 상태 진입: 메인 씬이 로드되었습니다.");



        GlobalEventManager.Publish(GameEventType.ShowBriefingUI);



    }







    public void Execute() { }







    public void Exit()



    {



        Debug.Log("[State] Briefing 상태 퇴장.");



        GlobalEventManager.Publish(GameEventType.HideBriefingUI);



    }



}



