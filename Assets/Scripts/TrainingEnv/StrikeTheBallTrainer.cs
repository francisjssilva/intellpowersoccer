﻿using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;

public class StrikeTheBallTrainer : Agent
{
    Rigidbody agentRBody;
    public AgentCore agentCore;
    public WheelchairAgentController controller;
    public GameEnvironmentInfo gameEnvironment;
    public GoalKeepTrainer goalKeepTrainer;
    float timeLeft;
    float timeOfFullPass;
    public Ball Ball;
    bool ballShooted;
    float angularVelocity;
    Vector3 ballPos;
    int numberOfTouches;
    float timeForTouches;
    bool unlockTouches;
    AgentCore goalKeeper;
    int site;
     

    void Start()
    {
        agentRBody = GetComponent<Rigidbody>();
        ballShooted = false;
        timeOfFullPass = 1f;
        timeLeft = 60f;
        angularVelocity = 0;
        ballPos = Vector3.zero;
        numberOfTouches = 0;
        timeForTouches = 0;
        goalKeeper = gameEnvironment.blueTeamAgents[0];
        site = Random.Range(0, 2);
        goalKeepTrainer.setSite(site);
    }

    void Update()
    {

        timeLeft -= Time.deltaTime;

        if(ballShooted){
            timeOfFullPass += Time.deltaTime;
        }

        if(timeLeft < 0){
            goalKeepTrainer.SetReward(2);
            goalKeepTrainer.Done();
        }

        timeForTouches += Time.deltaTime;
 
        if(timeForTouches >= 0.5f){
            unlockTouches = true;
        }
    }

    private void FixedUpdate() {
        if(agentOutOfPlay()){
            goalKeepTrainer.Done();
        }
        
        if(ballOutOfPlay()){
            goalKeepTrainer.Done();
        }

        checkAgentPos();
        checkBallPos();
    }

    public override void InitializeAgent() 
    {
        agentRBody = GetComponent<Rigidbody>();
        ballShooted = false;
        timeOfFullPass = 1f;
        timeLeft = 60f;
        angularVelocity = 0;
        ballPos = Vector3.zero;
        site = Random.Range(0, 2);
        goalKeepTrainer.setSite(site);
        goalKeeper = gameEnvironment.blueTeamAgents[0];
        
        positionBall();
        positionPlayers();
    }

    public override float[] Heuristic()
    {
        var action = new float[2];
        action[0] = Input.GetAxis("Horizontal");
        action[1] = Input.GetAxis("Vertical");
        return action;
    }

    public override void CollectObservations()
    {
        // Agent velocity
        AddVectorObs(agentRBody.angularVelocity.magnitude);

        AddVectorObs(agentCore.distanceToBall());
        AddVectorObs(angleBetweenAgentAndBall());

        AddVectorObs(numberOfTouches);
    }

    public override void AgentAction(float[] vectorAction)
    {
        controller.Controller(vectorAction);
    }

    public override void AgentReset()
    {        
        site = Random.Range(0, 2);
        goalKeepTrainer.setSite(site);
        stopAgents();
        positionBall();
        positionPlayers();
        
        ballShooted = false;
        timeOfFullPass = 1f;
        timeLeft = 60f;
        angularVelocity = 0;
        numberOfTouches = 0;
    }

    public void scoredRedGoal(){
        //Debug.Log("ENTRA RED");
        if(ballShooted)
            if(site < 1){
                SetReward((10.0f/timeOfFullPass) - 0.2f*numberOfTouches);
                //Debug.Log("GOAL SCORED. REWARD: " + ((10.0f/timeOfFullPass) - 0.2f*numberOfTouches));
                goalKeepTrainer.Done();
            }
    }

    public void scoredBlueGoal(){
        //Debug.Log("ENTRA BLUE");
        if(ballShooted)
            if(site > 0){
                SetReward((10.0f/timeOfFullPass) - 0.2f*numberOfTouches);
                //Debug.Log("GOAL SCORED. REWARD: " + ((10.0f/timeOfFullPass) - 0.2f*numberOfTouches));
                goalKeepTrainer.Done();
            }
    }

    public float AngleDir(Vector3 fwd, Vector3 targetDir){
        Vector3 perp = Vector3.Cross(fwd, targetDir);
        float dir = Vector3.Dot(perp, Vector3.up);

        return Mathf.Sign(dir);
    }
    
    public float angleBetweenAgentAndBall(){
        Vector3 agentToBallVec = Ball.transform.localPosition - agentCore.transform.localPosition;
        Vector3 agentToForwardVec = agentCore.transform.forward*-1;

        return Vector3.Angle(agentToForwardVec, agentToBallVec) * AngleDir(agentToForwardVec, agentToBallVec);
    }

    public void positionPlayers(){
        float z = Random.Range(-3.0f, 3.0f);

        if(site > 0){
            agentCore.transform.localPosition = new Vector3(10f, 0.25f, 0f);
            goalKeeper.transform.localPosition = new Vector3(15f, 0.25f, z);
        }else{
            agentCore.transform.localPosition = new Vector3(-10f, 0.25f, 0f);
            goalKeeper.transform.localPosition = new Vector3(-15f, 0.25f, z);
        }

        agentCore.transform.rotation = Quaternion.LookRotation(-(Ball.transform.localPosition - agentCore.transform.localPosition));
        goalKeeper.transform.rotation = Quaternion.LookRotation(-(Ball.transform.localPosition - goalKeeper.transform.localPosition));
    }

    public void positionBall(){

        if(site > 0){
            Ball.transform.localPosition = new Vector3(11.5f, 0.44f, 0);
        }else{
            Ball.transform.localPosition = new Vector3(-11.5f, 0.44f, 0);
        }

        ballPos = Ball.transform.localPosition;
        Ball.stopIt();
        
    }

    public void touchedBall(){
        if(unlockTouches){
            numberOfTouches += 1;
            unlockTouches = false;
            timeForTouches = 0;
        }

        //Debug.Log("NUMBER OF TOUCHES: " + numberOfTouches);
        if(agentCore.GetComponent<Rigidbody>().angularVelocity.magnitude > 1.7f){
            angularVelocity = agentRBody.angularVelocity.magnitude;
            //Debug.Log("Ball was kicked, REWARD: " + 30f / (61 - timeLeft));
            //SetReward(30f / (61 - timeLeft));
            SetReward(0.01f);
            ballShooted = true;
            goalKeepTrainer.oponentStriked();
        }
    }

    public void stopAgents(){
        foreach(AgentCore agent in gameEnvironment.redTeamAgents){
            agent.stopChair();
        }

        foreach(AgentCore agent in gameEnvironment.blueTeamAgents){
            agent.stopChair();
        }
    }

    public bool agentOutOfPlay(){
        if(agentCore.transform.localPosition.x > 16.5 || agentCore.transform.localPosition.x < -16.5){
            SetReward(-0.01f);
            return true;
        }
        else if(agentCore.transform.localPosition.z > 9.5 || agentCore.transform.localPosition.z < -9.5){
            SetReward(-0.01f);
            return true;
        }

        return false;
    }

    public bool ballOutOfPlay(){
        if(Ball.transform.localPosition.y < 0.2){
            SetReward(-0.01f);
            return true;
        }
        return false;
    }

    public void checkBallPos(){
        if(Vector3.Distance(Ball.transform.localPosition, ballPos) > 6){
            //AddReward(0.01f);
            goalKeepTrainer.SetReward(4);
            goalKeepTrainer.Done();
        }
    }

    public void checkAgentPos(){
        if(Vector3.Distance(agentCore.transform.localPosition, ballPos) > 3){
            SetReward(-0.5f);
            goalKeepTrainer.SetReward(4);
            goalKeepTrainer.Done();
        }
    }
}