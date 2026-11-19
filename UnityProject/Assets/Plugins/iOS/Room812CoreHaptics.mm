#import <UIKit/UIKit.h>

extern "C" {
    void Room812_PulseHaptic(int style)
    {
        if (@available(iOS 13.0, *)) {
            UIImpactFeedbackStyle s = UIImpactFeedbackStyleMedium;
            if (style == 0) s = UIImpactFeedbackStyleLight;
            else if (style == 2) s = UIImpactFeedbackStyleHeavy;
            UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
            [generator prepare];
            [generator impactOccurred];
        }
    }
}
