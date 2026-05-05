def detect_custom_gesture(landmarks):
    def get_dist(lm1, lm2):
        return ((lm1.x - lm2.x) ** 2 + (lm1.y - lm2.y) ** 2) ** 0.5

    index_is_open = get_dist(landmarks[8], landmarks[0]) > get_dist(landmarks[6], landmarks[0])
    middle_is_open = get_dist(landmarks[12], landmarks[0]) > get_dist(landmarks[10], landmarks[0])
    ring_is_open = get_dist(landmarks[16], landmarks[0]) > get_dist(landmarks[14], landmarks[0])
    pinky_is_open = get_dist(landmarks[20], landmarks[0]) > get_dist(landmarks[18], landmarks[0])
    
    thumb_is_open = get_dist(landmarks[4], landmarks[0]) > get_dist(landmarks[2], landmarks[0])
    
    if thumb_is_open and index_is_open and not middle_is_open and not ring_is_open and not pinky_is_open:
        return "Custom_Gun"
    
    # Add custom gestures
    
    if not thumb_is_open and index_is_open and middle_is_open and ring_is_open and pinky_is_open:
        return "Custom_Four"
    
    
    if thumb_is_open and not index_is_open and not middle_is_open and not ring_is_open and pinky_is_open:
        return "Custum_Phone"
    
    return None