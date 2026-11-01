#import <Foundation/Foundation.h>
#import <StoreKit/StoreKit.h>
#import <Security/Security.h>

extern "C" void UnitySendMessage(const char *, const char *, const char *);
static NSString *const ProductID = @"com.hiroki1989.ashfall.premium.unlock";
static NSString *const KeychainService = @"com.hiroki1989.ashfall.premium";
static NSString *receiver;
static BOOL revokedInSession;

static NSMutableDictionary *KeyQuery(void) {
    return [@{(__bridge id)kSecClass:(__bridge id)kSecClassGenericPassword,
              (__bridge id)kSecAttrService:KeychainService,
              (__bridge id)kSecAttrAccount:ProductID} mutableCopy];
}
static BOOL Owned(void) {
    if (revokedInSession) return NO;
    NSMutableDictionary *q = KeyQuery();
    q[(__bridge id)kSecReturnData] = @YES;
    CFTypeRef result = NULL;
    OSStatus s = SecItemCopyMatching((__bridge CFDictionaryRef)q, &result);
    if (result) CFRelease(result);
    return s == errSecSuccess;
}
static BOOL SaveOwned(void) {
    NSMutableDictionary *q = KeyQuery();
    q[(__bridge id)kSecValueData] = [@"1" dataUsingEncoding:NSUTF8StringEncoding];
    q[(__bridge id)kSecAttrAccessible] = (__bridge id)kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly;
    OSStatus s = SecItemAdd((__bridge CFDictionaryRef)q, NULL);
    BOOL saved = s == errSecSuccess || s == errSecDuplicateItem;
    if (saved) revokedInSession = NO;
    return saved;
}
static void Status(NSString *message) {
    if (receiver) UnitySendMessage(receiver.UTF8String, "OnPremiumStatus", message.UTF8String);
}
static void Price(NSString *message) {
    if (receiver) UnitySendMessage(receiver.UTF8String, "OnPremiumPrice", message.UTF8String);
}

@interface AshPremiumObserver : NSObject <SKPaymentTransactionObserver, SKProductsRequestDelegate>
@property(nonatomic,strong) SKProductsRequest *request;
@property(nonatomic,strong) SKProduct *product;
@property(nonatomic,assign) BOOL buying;
@property(nonatomic,assign) BOOL restoring;
@property(nonatomic,assign) BOOL restoreFound;
@property(nonatomic,assign) BOOL restoreSaveFailed;
@end
@implementation AshPremiumObserver
- (void)fetchProduct {
    if (self.request) return;
    self.request = [[SKProductsRequest alloc] initWithProductIdentifiers:[NSSet setWithObject:ProductID]];
    self.request.delegate = self;
    [self.request start];
}
- (void)buy {
    if (self.buying || self.restoring) { Status(@"Store operation in progress / 処理中です"); return; }
    if (Owned()) { Status(@"Already unlocked / 解放済みです"); return; }
    if (![SKPaymentQueue canMakePayments]) { Status(@"Purchases are unavailable / 購入を利用できません"); return; }
    self.buying = YES;
    if (self.product) [[SKPaymentQueue defaultQueue] addPayment:[SKPayment paymentWithProduct:self.product]];
    else [self fetchProduct];
}
- (void)productsRequest:(SKProductsRequest *)request didReceiveResponse:(SKProductsResponse *)response {
    SKProduct *product = response.products.firstObject;
    self.request = nil;
    if (!product) {
        Price(@"");
        if (self.buying) { self.buying = NO; Status(@"Product unavailable / 商品を取得できません"); }
        return;
    }
    self.product = product;
    NSNumberFormatter *formatter = [NSNumberFormatter new];
    formatter.numberStyle = NSNumberFormatterCurrencyStyle;
    formatter.locale = product.priceLocale;
    NSString *code = [product.priceLocale objectForKey:NSLocaleCurrencyCode];
    if (code.length > 0) {
        formatter.currencyCode = code;
        formatter.currencySymbol = @"";
        NSString *raw = product.price.stringValue;
        NSRange dot = [raw rangeOfString:@"."];
        if (dot.location != NSNotFound)
            formatter.maximumFractionDigits = MAX(formatter.maximumFractionDigits, raw.length - dot.location - 1);
        NSString *number = [[formatter stringFromNumber:product.price] stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]];
        Price(number.length > 0 ? [NSString stringWithFormat:@"%@ %@", code, number] : @"");
    } else Price(@"");
    if (self.buying) [[SKPaymentQueue defaultQueue] addPayment:[SKPayment paymentWithProduct:product]];
}
- (void)request:(SKRequest *)request didFailWithError:(NSError *)error {
    self.request = nil;
    if (self.buying) { self.buying = NO; Status(@"Store connection failed / ストアへの接続に失敗しました"); }
}
- (void)restore {
    if (self.buying || self.restoring) { Status(@"Store operation in progress / 処理中です"); return; }
    self.restoring = YES; self.restoreFound = NO; self.restoreSaveFailed = NO;
    [[SKPaymentQueue defaultQueue] restoreCompletedTransactions];
}
- (void)paymentQueue:(SKPaymentQueue *)queue updatedTransactions:(NSArray<SKPaymentTransaction *> *)transactions {
    for (SKPaymentTransaction *t in transactions) {
        if (![t.payment.productIdentifier isEqualToString:ProductID]) continue;
        if (t.transactionState == SKPaymentTransactionStatePurchased || t.transactionState == SKPaymentTransactionStateRestored) {
            BOOL saved = SaveOwned();
            if (t.transactionState == SKPaymentTransactionStateRestored) {
                self.restoreFound = YES;
                if (!saved) self.restoreSaveFailed = YES;
            }
            self.buying = NO;
            if (saved) { [queue finishTransaction:t]; Status(@"Premium unlocked / 解放しました"); }
            else Status(@"Could not save purchase; retry later / 購入を保存できません。後でもう一度お試しください");
        } else if (t.transactionState == SKPaymentTransactionStateFailed) {
            self.buying = NO;
            Status(t.error.code == SKErrorPaymentCancelled ? @"Purchase cancelled / 購入をキャンセルしました" : @"Purchase failed / 購入に失敗しました");
            [queue finishTransaction:t];
        } else if (t.transactionState == SKPaymentTransactionStateDeferred) {
            self.buying = NO; Status(@"Awaiting approval / 承認待ちです");
        }
    }
}
- (void)paymentQueueRestoreCompletedTransactionsFinished:(SKPaymentQueue *)queue {
    self.restoring = NO;
    Status(self.restoreSaveFailed ? @"Could not save restored purchase / 復元した購入を保存できません" : self.restoreFound ? @"Purchase restored / 購入を復元しました" : @"No purchase to restore / 復元できる購入はありません");
}
- (void)paymentQueue:(SKPaymentQueue *)queue restoreCompletedTransactionsFailedWithError:(NSError *)error {
    self.restoring = NO; Status(@"Restore failed / 復元に失敗しました");
}
- (void)paymentQueue:(SKPaymentQueue *)queue didRevokeEntitlementsForProductIdentifiers:(NSArray<NSString *> *)productIdentifiers {
    if (![productIdentifiers containsObject:ProductID]) return;
    revokedInSession = YES;
    OSStatus result = SecItemDelete((__bridge CFDictionaryRef)KeyQuery());
    if (self.restoring) self.restoreSaveFailed = YES;
    if (result == errSecSuccess || result == errSecItemNotFound)
        Status(@"Premium access revoked / プレミアム権利が取り消されました");
    else
        Status(@"Could not update premium access / プレミアム権利を更新できません");
}
@end
static AshPremiumObserver *observer;
extern "C" void AshPremiumStart(const char *name) {
    receiver = [[NSString stringWithUTF8String:name] copy];
    if (!observer) { observer = [AshPremiumObserver new]; [[SKPaymentQueue defaultQueue] addTransactionObserver:observer]; [observer fetchProduct]; }
}
extern "C" int AshPremiumIsUnlocked(void) { return Owned() ? 1 : 0; }
extern "C" void AshPremiumPurchase(void) { [observer buy]; }
extern "C" void AshPremiumRestore(void) { [observer restore]; }
