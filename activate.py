import requests
import time
import json
import math
from coppeliasim_zmqremoteapi_client import RemoteAPIClient

# Web server code
url = "http://localhost:8000/"
start_time = time.time()
end_time = start_time + 20  # 20 seconds

# CoppeliaSim code
try:
    client = RemoteAPIClient()
    sim = client.require('sim')
except Exception as e:
    print(f"Error initializing RemoteAPIClient: {e}")
    exit(1)

try:
    sim.startSimulation()
except Exception as e:
    print(f"Error starting simulation: {e}")
    exit(1)

# Function to find all joint objects in the scene
def find_joint_objects():
    joints = []
    index = 0
    while True:
        joint = sim.getObjects(index, sim.object_joint_type)
        if joint == -1:
            break
        joints.append(joint)
        index += 1
    return joints

jointHandles = find_joint_objects()
is_gripper_open = True

def open_gripper():
    global is_gripper_open
    if not is_gripper_open:
        for i in range(0, len(jointHandles), 2):
            sim.setJointTargetPosition(jointHandles[i], 0.5 * math.pi)
            sim.setJointTargetPosition(jointHandles[i+1], 0.5 * math.pi)
        is_gripper_open = True
        print("Gripper opened")

def close_gripper():
    global is_gripper_open
    if is_gripper_open:
        for i in range(0, len(jointHandles), 2):
            sim.setJointTargetPosition(jointHandles[i], -0.5 * math.pi)
            sim.setJointTargetPosition(jointHandles[i+1], -0.5 * math.pi)
        is_gripper_open = False
        print("Gripper closed")

last_button_state = False

while time.time() < end_time:
    try:
        response = requests.get(url)
        print(f"Raw response: {response.text}")  # Log da resposta bruta
        dict_data = json.loads(response.text)  # Tenta decodificar o JSON
        current_button_state = dict_data.get("isButtonPressed", False)

        # Check for state change
        if current_button_state and not last_button_state:
            open_gripper()  # Open when pressed
        elif not current_button_state and last_button_state:
            close_gripper()  # Close when released

        last_button_state = current_button_state  # Update last state

    except requests.exceptions.RequestException as e:
        print(f"Error: {e}")
    except json.JSONDecodeError as e:
        print(f"JSON decoding error: {e}")
    except ValueError as e:
        print(f"Value error: {e}")

    time.sleep(0.1)  # Short wait for responsiveness
