# Component A — Test Results & Verification Evidence

> **CoreGrid | Asset Registry and QR Identification**

| Document Information | Details |
|---|---|
| **Component** | Asset Registry and QR Identification |
| **Requirements** | FR-016–FR-032 |
| **Test Date** | 29 September 2026 |
| **Web Application** | CoreGrid React Frontend |
| **Mobile Application** | CoreGrid Mobile |
| **Mobile Environment** | Android Pixel 6 Emulator (`emulator-5554`) |
| **Document Status** | Final Test & Verification Evidence |

---

## 1. Executive Summary

This document provides the consolidated test results and implementation evidence for **Component A — Asset Registry and QR Identification**.

The evidence package combines:

- React web application feature evidence
- React frontend automated test results
- Flutter mobile automated test results
- Dedicated Flutter scanner widget tests
- Flutter static analysis
- Android Pixel 6 emulator verification
- Individual evidence links for key asset workflows

### 1.1 Component A Coverage

| Functional Area | Web | Mobile | Automated Tests |
|---|:---:|:---:|:---:|
| Dashboard / asset overview | ✓ | ✓ | ✓ |
| Asset registration | ✓ | — | ✓ |
| Asset configuration | ✓ | — | ✓ |
| Asset search and lookup | ✓ | ✓ | ✓ |
| QR identification | ✓ | ✓ | ✓ |
| Asset details | ✓ | ✓ | ✓ |
| Asset condition update | ✓ | ✓ | ✓ |
| Asset history | ✓ | ✓ | ✓ |
| Physical asset verification | — | ✓ | ✓ |
| Scanner permission handling | — | ✓ | ✓ |
| Unknown QR handling | — | ✓ | ✓ |
| Offline recovery | — | ✓ | ✓ |
| Manual-entry fallback | — | ✓ | ✓ |

### 1.2 Overall Test Status

| Area | Passed | Failed | Status |
|---|---:|---:|---|
| React frontend tests | **113** | **0** | **Passed** |
| Flutter mobile tests | **62** | **0** | **Passed** |
| Scanner widget tests | **7** | **0** | **Passed** |
| Flutter static analysis | — | **0 issues** | **Passed** |
| React web feature evidence | Evidence attached | — | **Verified** |
| Android emulator workflows | Evidence attached | — | **Verified** |

---

# 2. React Web Application — Feature Evidence

This section records the implemented **React web application** evidence for Component A.

The evidence demonstrates the main web-based asset management workflows, including dashboard monitoring, asset registration, asset configuration, QR identification, asset viewing, and asset history.

## 2.1 React Web Feature Evidence Index

| # | Feature | Evidence | What the Evidence Demonstrates |
|---:|---|---|---|
| 1 | **Dashboard** | [View Evidence](https://drive.google.com/file/d/1ePfDXrYiN7Yx87mdxDWquw_lRhQYeaAH/view?usp=sharing) | Asset totals, active assets, maintenance, transfers, disposals, discrepancies, approvals, department distribution and condition overview |
| 2 | **Asset Register** | [View Evidence](https://drive.google.com/file/d/1GKW5_lYiIbWez7SMF5wsGzTOTR7ZlWzM/view?usp=sharing) | Asset listing, filtering, status, condition, department, location and acquisition information |
| 3 | **Asset View** | [View Evidence](https://drive.google.com/file/d/1yzDF2IE2hKuwyhjzmG0nLWAWc8SebHg2/view?usp=sharing) | Asset details, department, location, condition, acquisition data, residual value and QR payload |
| 4 | **Asset History** | [View Evidence](https://drive.google.com/file/d/16SWSN3RuRwjNltTXFR86_q3_ycEwiyuO/view?usp=sharing) | Asset lifecycle events, condition changes, maintenance records and verification events |
| 5 | **Asset Configuration** | [View Evidence](https://drive.google.com/file/d/1sVzACpS-lApur2xRy1QVKK_6O_IdZnYs/view?usp=sharing) | Categories, asset types, custom attributes and configuration management |
| 6 | **QR Code** | [View Evidence](https://drive.google.com/file/d/1PopvraK23yvPzbeIjrwLoQ4QsJUEc1cu/view?usp=sharing) | Generated QR label / QR identification functionality |
| 7 | **Register New Asset** | [View Evidence](https://drive.google.com/file/d/1lxhASzBZff45-YhQlH10gQJIfQfoyNK_/view?usp=sharing) | Dynamic asset registration form, asset type selection, condition, assignment and purchase/valuation fields |

---

## 2.2 Dashboard

The CoreGrid dashboard provides an operational overview of the asset registry.

The evidence includes:

- Total assets
- Active assets
- Assets under maintenance
- Pending transfers
- Pending disposals
- Open discrepancies
- Workflows awaiting approval
- Assets by department
- Assets by condition
- Maintenance cost overview

### Evidence

[**View React Dashboard Evidence →**](https://drive.google.com/file/d/1ePfDXrYiN7Yx87mdxDWquw_lRhQYeaAH/view?usp=sharing)

### Verification

The dashboard demonstrates that the web application provides a consolidated view of the organization's asset status and operational information.

---

## 2.3 Asset Register

The Asset Register provides a centralized view of registered assets.

The evidence demonstrates:

- Asset code
- Asset name
- Asset type
- Department
- Location
- Status
- Condition
- Acquisition cost
- Search and filtering controls

### Evidence

[**View React Asset Register Evidence →**](https://drive.google.com/file/d/1GKW5_lYiIbWez7SMF5wsGzTOTR7ZlWzM/view?usp=sharing)

### Verification

The Asset Register provides a structured interface for viewing, filtering and tracking asset records.

---

## 2.4 Asset View

The Asset View displays the detailed information associated with an individual asset.

The evidence demonstrates:

- Asset information
- Department
- Location
- Current condition
- Acquisition date
- Acquisition cost
- Residual value
- QR payload
- Generated QR code
- Condition update controls

### Evidence

[**View React Asset View Evidence →**](https://drive.google.com/file/d/1yzDF2IE2hKuwyhjzmG0nLWAWc8SebHg2/view?usp=sharing)

### Verification

The Asset View provides the detailed lifecycle and identification information required for an individual asset record.

---

## 2.5 Asset History

The Asset History view provides a chronological record of significant asset events.

The evidence demonstrates events including:

- Status changes
- Field amendments
- Condition changes
- Maintenance records
- Verification events
- Resolved verification discrepancies

### Evidence

[**View React Asset History Evidence →**](https://drive.google.com/file/d/16SWSN3RuRwjNltTXFR86_q3_ycEwiyuO/view?usp=sharing)

### Verification

The history view provides traceability of changes and operational events associated with an asset.

---

## 2.6 Asset Configuration

The Asset Configuration area provides administration of asset categories, types and custom attribute definitions.

The evidence demonstrates:

- Asset categories
- Asset types
- Custom attributes
- Category-level asset counts
- Asset-type counts
- Configuration management actions

### Evidence

[**View React Asset Configuration Evidence →**](https://drive.google.com/file/d/1sVzACpS-lApur2xRy1QVKK_6O_IdZnYs/view?usp=sharing)

### Verification

The configuration interface supports the setup of the asset structure required for dynamic asset registration.

---

## 2.7 QR Code / QR Identification

The React web application provides QR code generation and asset identification functionality.

The evidence demonstrates:

- QR code generation
- QR payload associated with an asset
- Printable asset label functionality
- QR-based asset identification workflow

### Evidence

[**View React QR Code Evidence →**](https://drive.google.com/file/d/1PopvraK23yvPzbeIjrwLoQ4QsJUEc1cu/view?usp=sharing)

### Verification

The QR functionality provides an identification mechanism linking the physical asset label to the corresponding asset record.

> **Evidence note:** The web evidence demonstrates the QR code/label and identification workflow. The dedicated Flutter scanner tests in Section 5 provide automated evidence for camera success, permission refusal, unknown QR handling, offline recovery and manual-entry fallback.

---

## 2.8 Register New Asset

The Register New Asset screen provides the asset creation workflow.

The evidence demonstrates:

- Asset type selection
- Asset name
- Initial condition
- Department assignment
- Location selection
- Purchase date
- Purchase cost
- Residual-value calculation context
- Auto-generated asset code preview

### Evidence

[**View React Register New Asset Evidence →**](https://drive.google.com/file/d/1lxhASzBZff45-YhQlH10gQJIfQfoyNK_/view?usp=sharing)

### Verification

The registration interface provides the required fields for creating an asset record and assigning it to the appropriate organizational structure.

---

# 3. React Frontend Automated Test Results

The final React frontend test execution completed successfully.

## 3.1 Execution Summary

| Metric | Result |
|---|---:|
| Test files passed | **23** |
| Tests passed | **113** |
| Failures | **0** |
| Overall status | **Passed** |

## 3.2 Asset Test Files

| Test File | Tests | Result |
|---|---:|---|
| `AssetComponents.test.tsx` | 10 | Passed |
| `AssetModals.test.tsx` | 8 | Passed |
| `format.test.ts` | 7 | Passed |
| `qrcode.test.ts` | 2 | Passed |
| `depreciation.test.ts` | 5 | Passed |

## 3.3 Latest React Frontend Test Evidence

[**View React Frontend Test Results →**](https://drive.google.com/file/d/1AcwI7uybg5W3gVakgnbGaW3MxtO1GrCD/view?usp=sharing)

---

# 4. React Asset Test Coverage

## 4.1 `AssetModals.test.tsx`

The test suite covers:

- Create Asset Type
- Edit Asset Type
- Edit Asset Attribute
- Asset Detail Modal
- Category Detail Modal
- Asset History Modal
- Attribute rule parsing
- Condition update permissions
- Asset history rendering

## 4.2 `AssetComponents.test.tsx`

The test suite covers:

- Create Category
- Edit Category
- Create custom Attribute
- Confirm Delete behaviour
- Pending deletion state
- Asset Summary rendering

## 4.3 `format.test.ts`

The test suite covers:

- `formatCurrency()`
- `formatDate()`
- `formatAttributeValue()`

## 4.4 `qrcode.test.ts`

The test suite covers:

- `generateQrDataUrl()`
- `downloadPrintableLabel()`

## 4.5 `depreciation.test.ts`

The test suite covers:

- Straight-line depreciation
- Residual value limits
- Local date formatting

---

# 5. Flutter Mobile Automated Test Results

The final Flutter/mobile test suite completed successfully.

## 5.1 Execution Summary

| Metric | Result |
|---|---:|
| Tests passed | **62** |
| Failures | **0** |
| Overall status | **All tests passed** |

## 5.2 Mobile Test Coverage

The automated mobile tests include coverage for:

- Asset search
- Asset lookup
- Asset verification
- Asset details
- Asset condition
- Dashboard asset lookup

## 5.3 Mobile Automated Test Evidence

[**View Mobile All Tests Passed Evidence →**](https://drive.google.com/file/d/134RExG5FODj916yAVa8vByqqos2uOy1y/view?usp=sharing)

---

# 6. Flutter Scanner Widget Test

A dedicated Flutter widget test was added for:

```text
test/features/scan/scan_asset_screen_test.dart
```

The test verifies the required scanner scenarios for `ScanAssetScreen`.

## 6.1 Scanner Test Scenarios

| Scenario | Verification | Result |
|---|---|---|
| Camera success | Valid QR code `AST-00042` resolves the asset and navigates to the asset record | **Passed** |
| Camera success — identify mode | Resolved `AssetDetail` is returned in identify mode | **Passed** |
| Permission refusal | Permission-denied UI, Open Settings option, and manual-entry fallback are displayed | **Passed** |
| Unknown code | Unknown QR code / HTTP 404 displays the expected error and Scan Again option | **Passed** |
| Offline recovery | Network failure displays the offline message and recovery works after connectivity is restored | **Passed** |
| Manual entry — standard mode | Entered code navigates to the asset list | **Passed** |
| Manual entry — identify mode | Entered code is resolved through `AssetsApi` and the asset is returned | **Passed** |

## 6.2 Scanner Test Execution

Command:

```bash
flutter test test/features/scan/scan_asset_screen_test.dart -r expanded
```

Result:

```text
+7: All tests passed!
```

## 6.3 Scanner Test Summary

| Metric | Result |
|---|---:|
| Scanner tests | **7** |
| Passed | **7** |
| Failed | **0** |
| Status | **Passed** |

## 6.4 Scanner Test Source

[`scan_asset_screen_test.dart`](https://github.com/CoreGrid-org/coregrid-mobile/blob/development/test/features/scan/scan_asset_screen_test.dart)

## 6.5 Scanner Test Execution Evidence

[**View Flutter Scanner Widget Test Evidence →**](https://drive.google.com/file/d/1lJN7bFFTr2BjxPrjxtVnaimKkLCnwy61/view?usp=sharing)

## 6.6 Static Analysis

Static analysis was completed successfully.

Command:

```bash
flutter analyze
```

Result:

```text
No issues found!
```

---

# 7. Mobile Device / Emulator Verification

Manual mobile verification was performed using the Android Pixel 6 emulator.

| Item | Details |
|---|---|
| Platform | Android |
| Device | Pixel 6 Emulator |
| Emulator ID | `emulator-5554` |
| Application | CoreGrid Mobile |
| Test Date | 29 September 2026 |

Each workflow has separate evidence so that individual Component A features can be reviewed independently.

---

## 7.1 Home / Asset Entry Point

The CoreGrid Mobile home screen provides the main entry point for asset-related workflows.

Available entry points include:

- Scan QR code
- Asset code or name search
- Verification tasks
- Asset workload information

### Evidence

[**View Home Screen Evidence →**](https://drive.google.com/file/d/1FRwTpva_ViYRN9HFcl_3QXgOdreOAe0n/view?usp=sharing)

### Verification

The evidence demonstrates the main mobile entry point for asset lookup and verification workflows.

---

## 7.2 QR Scan

The mobile application provides a **Scan QR Code** workflow for identifying an asset.

### Evidence

[**View QR Scan Evidence →**](https://drive.google.com/file/d/16iHvPSCblqaJyvbHnzS5CsHPSaU5Jsvn/view?usp=sharing)

### Verification

The evidence demonstrates the QR scanning entry point within the CoreGrid Mobile application.

> **Evidence note:** The current manual evidence does not include a camera-frame screenshot showing the QR code being detected. The dedicated Flutter scanner widget tests in Section 6 provide automated evidence for successful QR handling.

---

## 7.3 Asset Search

The mobile application provides an asset search function that allows the user to search by asset code or name.

### Evidence

[**View Asset Search Evidence →**](https://drive.google.com/file/d/1dN1eAyXwXFMBoCoPN8YocE3eTW9vGkSS/view?usp=sharing)

### Verification

The evidence demonstrates the asset search interface and lookup workflow.

---

## 7.4 Asset Lookup — AAA

The `AAA_Lookup` evidence demonstrates the lookup of asset `AAA`.

### Evidence

[**View AAA Lookup Evidence →**](https://drive.google.com/file/d/1DgfTd4p6wh3cUgUGHdMJ5Z4nV3fTx6HR/view?usp=sharing)

### Verification

The application resolves asset `AAA` and displays the corresponding asset information.

---

## 7.5 Asset Lookup — Asset Details

The `Asset_Lookup` evidence provides the detailed asset record after the asset has been located.

### Evidence

[**View Asset Lookup Details Evidence →**](https://drive.google.com/file/d/1rDVjHButhxeXANYUaGY4WKbQa80e7epf/view?usp=sharing)

### Asset Information

The asset record includes:

- Asset code
- Asset name
- Asset type
- Department
- Location
- Acquisition information
- Residual value
- Current condition

### Verification

The mobile application displays the selected asset and its details.

---

## 7.6 Condition Update

The mobile application provides an **Update Condition** action from the asset details screen.

Available conditions include:

- New
- Good
- Fair
- Poor
- Unserviceable

### Evidence

[**View Condition Update Evidence →**](https://drive.google.com/file/d/1fCdDIxDqPE37o6Hk-yhKn5qzJmKSOjcn/view?usp=sharing)

### Verification

The evidence demonstrates that the asset condition can be selected and saved through the mobile application.

---

## 7.7 Asset History

The asset history records changes and verification events associated with the asset.

### Evidence

[**View Asset History Evidence →**](https://drive.google.com/file/d/1h4A6zpPwNm1psfqiOy-zxNaqO8qav6q7/view?usp=sharing)

### Recorded Events

The evidence demonstrates history entries such as:

- Condition changes
- Verification events
- Maintenance-related events

### Verification

The mobile application displays the asset lifecycle history and recorded changes.

---

## 7.8 Asset Verification

The mobile application provides a dedicated physical asset verification workflow.

The workflow allows the user to:

1. Open the asset record.
2. Scan the asset QR label.
3. Confirm whether the asset is present.
4. Select the asset location.
5. Select the observed condition.
6. Submit the verification.
7. Raise a discrepancy when required.

### Evidence

[**View Asset Verification Evidence →**](https://drive.google.com/file/d/1wIFQW-k49PXIl1GHVJfEyN-5216sND7f/view?usp=sharing)

### Verification

The evidence demonstrates the controls required for physical asset verification.

---

## 7.9 Verify AAA Asset

The `Veify_AAA_Asset` evidence demonstrates the verification workflow for asset `AAA`.

### Evidence

[**View Verify AAA Asset Evidence →**](https://drive.google.com/file/d/1EX77jpT2afSV0bvV22omAvEinqFMSdpe/view?usp=sharing)

### Verification

The selected asset can be verified through the mobile application.

The workflow allows confirmation of:

- Asset presence
- Location
- Observed condition

---

# 8. Mobile Evidence Index

| # | Mobile Workflow | Evidence | Status |
|---:|---|---|---|
| 1 | Home / Asset Entry | [View Evidence](https://drive.google.com/file/d/1FRwTpva_ViYRN9HFcl_3QXgOdreOAe0n/view?usp=sharing) | **Demonstrated** |
| 2 | QR Scan | [View Evidence](https://drive.google.com/file/d/16iHvPSCblqaJyvbHnzS5CsHPSaU5Jsvn/view?usp=sharing) | **Demonstrated** |
| 3 | Asset Search | [View Evidence](https://drive.google.com/file/d/1dN1eAyXwXFMBoCoPN8YocE3eTW9vGkSS/view?usp=sharing) | **Demonstrated** |
| 4 | Asset Lookup — AAA | [View Evidence](https://drive.google.com/file/d/1DgfTd4p6wh3cUgUGHdMJ5Z4nV3fTx6HR/view?usp=sharing) | **Demonstrated** |
| 5 | Asset Lookup — Details | [View Evidence](https://drive.google.com/file/d/1rDVjHButhxeXANYUaGY4WKbQa80e7epf/view?usp=sharing) | **Demonstrated** |
| 6 | Condition Update | [View Evidence](https://drive.google.com/file/d/1fCdDIxDqPE37o6Hk-yhKn5qzJmKSOjcn/view?usp=sharing) | **Demonstrated** |
| 7 | Asset History | [View Evidence](https://drive.google.com/file/d/1h4A6zpPwNm1psfqiOy-zxNaqO8qav6q7/view?usp=sharing) | **Demonstrated** |
| 8 | Asset Verification | [View Evidence](https://drive.google.com/file/d/1wIFQW-k49PXIl1GHVJfEyN-5216sND7f/view?usp=sharing) | **Demonstrated** |
| 9 | Verify AAA Asset | [View Evidence](https://drive.google.com/file/d/1EX77jpT2afSV0bvV22omAvEinqFMSdpe/view?usp=sharing) | **Demonstrated** |

---

# 9. Device / Emulator Record

| Item | Value |
|---|---|
| Platform | Android |
| Device | Pixel 6 |
| Emulator ID | `emulator-5554` |
| Application | CoreGrid Mobile |
| Test Date | 29 September 2026 |

The individual evidence links in Sections 7–8 provide the recorded device/emulator evidence for each mobile workflow.

---

# 10. Consolidated Test Execution Record

| Date | Environment | Test / Procedure | Passed | Failed | Evidence |
|---|---|---|---:|---:|---|
| 2026-09-29 | React / Vitest | Frontend test suite | **113** | **0** | [React Frontend Test Results](https://drive.google.com/file/d/1AcwI7uybg5W3gVakgnbGaW3MxtO1GrCD/view?usp=sharing) |
| 2026-09-29 | Flutter / macOS | `flutter test` | **62** | **0** | [Mobile Test Evidence](https://drive.google.com/file/d/134RExG5FODj916yAVa8vByqqos2uOy1y/view?usp=sharing) |
| 2026-09-29 | Flutter / macOS | Scanner widget test | **7** | **0** | [Scanner Test Evidence](https://drive.google.com/file/d/1lJN7bFFTr2BjxPrjxtVnaimKkLCnwy61/view?usp=sharing) |
| 2026-09-29 | Flutter / macOS | Static analysis | — | **0 issues** | — |
| 2026-09-29 | React Web | Feature evidence review | — | — | [React Web Feature Evidence Index](#2-react-web-application--feature-evidence) |
| 2026-09-29 | Android Pixel 6 Emulator | Manual mobile workflows | — | — | [Mobile Evidence Index](#8-mobile-evidence-index) |

---

# 11. Final Verification Status

| Platform | Test / Evidence | Passed | Failed | Status |
|---|---|---:|---:|---|
| React Web | Feature implementation evidence | — | — | **Evidence Attached** |
| React | Frontend automated tests | **113** | **0** | **Passed** |
| Flutter | Mobile automated tests | **62** | **0** | **Passed** |
| Flutter | Scanner widget tests | **7** | **0** | **Passed** |
| Flutter | Static analysis | — | **0 issues** | **Passed** |
| Android Emulator | Manual Component A workflows | — | — | **Evidence Attached** |

---

# 12. Evidence Coverage Matrix

| Component A Area | React Web Evidence | React Automated Tests | Flutter Automated Tests | Mobile Manual Evidence |
|---|:---:|:---:|:---:|:---:|
| Dashboard | ✓ | ✓ | ✓ | ✓ |
| Asset registration | ✓ | ✓ | — | ✓ |
| Asset configuration | ✓ | ✓ | — | — |
| Asset search | ✓ | ✓ | ✓ | ✓ |
| Asset lookup | ✓ | ✓ | ✓ | ✓ |
| QR identification | ✓ | ✓ | ✓ | ✓ |
| Asset details | ✓ | ✓ | ✓ | ✓ |
| Condition update | ✓ | ✓ | ✓ | ✓ |
| Asset history | ✓ | ✓ | ✓ | ✓ |
| Physical verification | — | — | ✓ | ✓ |
| Scanner permission handling | — | — | ✓ | — |
| Unknown QR handling | — | — | ✓ | — |
| Offline recovery | — | — | ✓ | — |
| Manual-entry fallback | — | — | ✓ | — |

---

# 13. Evidence Reference Directory

## React Web Evidence

1. [Dashboard](https://drive.google.com/file/d/1ePfDXrYiN7Yx87mdxDWquw_lRhQYeaAH/view?usp=sharing)
2. [Asset Register](https://drive.google.com/file/d/1GKW5_lYiIbWez7SMF5wsGzTOTR7ZlWzM/view?usp=sharing)
3. [Asset View](https://drive.google.com/file/d/1yzDF2IE2hKuwyhjzmG0nLWAWc8SebHg2/view?usp=sharing)
4. [Asset History](https://drive.google.com/file/d/16SWSN3RuRwjNltTXFR86_q3_ycEwiyuO/view?usp=sharing)
5. [Asset Configuration](https://drive.google.com/file/d/1sVzACpS-lApur2xRy1QVKK_6O_IdZnYs/view?usp=sharing)
6. [QR Code](https://drive.google.com/file/d/1PopvraK23yvPzbeIjrwLoQ4QsJUEc1cu/view?usp=sharing)
7. [Register New Asset](https://drive.google.com/file/d/1lxhASzBZff45-YhQlH10gQJIfQfoyNK_/view?usp=sharing)
8. [React Frontend Test Results](https://drive.google.com/file/d/1AcwI7uybg5W3gVakgnbGaW3MxtO1GrCD/view?usp=sharing)

## Mobile Automated Evidence

1. [Mobile All Tests Passed](https://drive.google.com/file/d/134RExG5FODj916yAVa8vByqqos2uOy1y/view?usp=sharing)
2. [Flutter Scanner Widget Test](https://drive.google.com/file/d/1lJN7bFFTr2BjxPrjxtVnaimKkLCnwy61/view?usp=sharing)

## Mobile Manual Evidence

1. [Home](https://drive.google.com/file/d/1FRwTpva_ViYRN9HFcl_3QXgOdreOAe0n/view?usp=sharing)
2. [QR Scan](https://drive.google.com/file/d/16iHvPSCblqaJyvbHnzS5CsHPSaU5Jsvn/view?usp=sharing)
3. [Asset Search](https://drive.google.com/file/d/1dN1eAyXwXFMBoCoPN8YocE3eTW9vGkSS/view?usp=sharing)
4. [AAA Lookup](https://drive.google.com/file/d/1DgfTd4p6wh3cUgUGHdMJ5Z4nV3fTx6HR/view?usp=sharing)
5. [Asset Lookup Details](https://drive.google.com/file/d/1rDVjHButhxeXANYUaGY4WKbQa80e7epf/view?usp=sharing)
6. [Condition Update](https://drive.google.com/file/d/1fCdDIxDqPE37o6Hk-yhKn5qzJmKSOjcn/view?usp=sharing)
7. [Asset History](https://drive.google.com/file/d/1h4A6zpPwNm1psfqiOy-zxNaqO8qav6q7/view?usp=sharing)
8. [Asset Verification](https://drive.google.com/file/d/1wIFQW-k49PXIl1GHVJfEyN-5216sND7f/view?usp=sharing)
9. [Verify AAA Asset](https://drive.google.com/file/d/1EX77jpT2afSV0bvV22omAvEinqFMSdpe/view?usp=sharing)

---

# 14. Final Evidence Statement

Component A has been verified through a combination of **React web implementation evidence, automated frontend testing, automated Flutter testing, dedicated scanner widget testing, static analysis, and manual Android emulator verification**.

### Recorded Results

| Verification Area | Result |
|---|---|
| React web feature evidence | **Evidence attached** |
| React frontend automated tests | **113 passed / 0 failures** |
| Flutter mobile automated tests | **62 passed / 0 failures** |
| Flutter scanner widget tests | **7 passed / 0 failures** |
| Flutter static analysis | **No issues found** |
| Mobile workflow evidence | **Individual evidence attached** |

The evidence package provides traceable implementation and test evidence for review of **Component A requirements FR-016–FR-032**.

---

## Document Control

| Field | Value |
|---|---|
| **Document** | Component A — Test Results & Verification Evidence |
| **Component** | Asset Registry and QR Identification |
| **Application** | CoreGrid Web + CoreGrid Mobile |
| **Requirements** | FR-016–FR-032 |
| **Evidence Date** | 29 September 2026 |
| **Status** | **Final** |
| **Last Updated** | 29 September 2026 |

> **End of Component A Evidence Document**
