# CoreGrid Role and Platform Guide
## Sri Lankan Public-Sector Examples

This document explains why each CoreGrid role uses React, Flutter, or both. It also provides practical examples from Sri Lankan education, healthcare, and transport environments.

> **Document purpose:** This guide explains requirements already defined in the SRS. It does not introduce new functional requirements. If this guide conflicts with SRS §3.4.1, the SRS takes priority.

---

## 1. Platform Model

| Role | React Web | Flutter Mobile | Primary Work |
|---|:---:|:---:|---|
| Administrator | Yes | No | Administration and approvals |
| Auditor | Yes | No | Audit, verification review, and reporting |
| Inventory Officer | Yes | Yes | Asset management and field operations |
| Staff | No | Yes | Asset lookup and fault reporting |


**Platform principle**

- Office-based management and review: **React**
- Field-based work: **Flutter**
- Office and field responsibilities: **React + Flutter**

---

## 2. Role Responsibilities

### 2.1 Administrator

## Platform

**React Web only**

Flutter: **Not allowed**

## What Does the Administrator Do?

The Administrator mainly works from an **office computer**.

Common tasks include:

- Create users and assign roles
- Create departments
- Create locations
- Create asset categories
- Create asset types
- Define custom asset attributes
- Approve asset disposals
- Approve maintenance costs
- Manage organisation settings

### Example

The hospital needs a new asset type called **Ventilator**.

The Administrator can configure:

- Asset type name
- Serial number information
- Calibration due date
- Warranty information
- Useful life

## Why React?

These tasks normally need:

- A large screen
- A keyboard
- Tables and forms
- Full asset information
- Careful checking before approval

The Administrator normally performs this work while sitting at a desk.

## Why No Flutter?

The Administrator does not normally need to:

- Scan QR codes
- Take asset photos
- Walk around checking assets
- Complete normal field verification tasks

## What the Administrator Cannot Do

The Administrator is not intended to perform normal field activities such as:

- Field QR scanning
- Physical asset verification assigned to an Inventory Officer
- Routine mobile field tasks

This keeps the Administrator focused on system management and approval work.

## Typical Day

**Login to React → Check system information → Manage users/configuration → Review requests → Approve required actions**

### Simple Idea

> **Administrator = Manage the system → React**

---

### 2.2 Auditor

## Platform

**React Web only**

Flutter: **Not allowed**

## What Does the Auditor Do?

The Auditor can:

- Create verification campaigns
- Review campaign results
- Review discrepancies
- Check audit history
- Resolve discrepancies
- Generate reports
- Export reports

### Example Result

| Verification ResultCount |     |
| ------------------------ | --- |
| Verified                 | 340 |
| Missing                  | 3   |
| Condition Mismatch       | 5   |
| Location Mismatch        | 4   |

The Auditor reviews these results and investigates problems.

## Why React?

Auditors need to work with:

- Large tables
- Reports
- Asset history
- Verification results
- Discrepancies
- Audit records

A desktop screen is better for comparing and reviewing this information.

## Why No Flutter?

The Auditor **reviews the verification**.

The Inventory Officer **performs the physical verification**.

Keeping these responsibilities separate helps maintain audit independence.

## What the Auditor Cannot Do

The Auditor should not perform the Inventory Officer's normal field work.

For example:

- Physically verify assigned assets as the responsible Inventory Officer
- Complete Inventory Officer field tasks
- Perform normal asset management activities
- Perform Administrator configuration work

This keeps the person performing the verification separate from the person reviewing its results.

## Typical Day

**Login to React → Create/review campaign → Check results → Review discrepancies → Resolve issues → Generate report**

### Simple Idea

> **Auditor = Check and review the system → React**

---

### 2.3 Inventory Officer

## Platform

**React + Flutter**

This role needs **both platforms**.

## React — Office Work

The Inventory Officer can use React to:

- Register assets
- Search assets
- View asset records
- Create maintenance records
- Assign maintenance work
- Enter estimated costs
- Complete maintenance records
- View asset history

### Example

Bus **NB-4471** needs new brake pads.

The Inventory Officer can use React:

> **Open Asset → Create Maintenance → Assign Mechanic → Enter Estimated Cost**

---

## Flutter — Field Work

Later, the same Inventory Officer walks into the bus yard.

They can use Flutter to:

- Scan QR codes
- Open an asset record
- Check the physical location
- Check asset condition
- Complete verification tasks
- Confirm asset transfers
- Confirm physical receipt

### Example

> **Scan NB-4471 → Open Bus Record → Check Location → Check Condition → Confirm**

## Why Both?

The same person performs both office work and field work.

| TaskBest Platform        |         |
| ------------------------ | ------- |
| Register assets          | React   |
| Maintenance management   | React   |
| Enter costs              | React   |
| Search large asset lists | React   |
| Scan QR codes            | Flutter |
| Physical verification    | Flutter |
| Check assets in the yard | Flutter |
| Confirm physical receipt | Flutter |

## What the Inventory Officer Cannot Do

The Inventory Officer handles assets and field operations but should not perform Administrator-only or Auditor-only responsibilities.

For example:

- Create or manage system users as an Administrator
- Change organisation-level system configuration without permission
- Perform Administrator-only approvals
- Act as the independent Auditor reviewing their own verification work

This keeps operational work, administration, and auditing separate.

## Typical Day

**React in office → Check assets and maintenance → Go to yard → Open Flutter → Scan assets → Verify condition/location → Complete field tasks**

### Simple Idea

> **Inventory Officer = Manage and physically check assets → React + Flutter**

---

### 2.4 Staff

## Platform

**Flutter Mobile only**

React: **Not allowed in the final platform design**

## What Does Staff Do?

Using Flutter:

**1. Open CoreGrid**

↓

**2. Scan the asset QR code**

↓

**3. View the asset**

↓

**4. Take a photo of the problem**

↓

**5. Describe the problem**

↓

**6. Submit the maintenance request**

The employee does not need to leave the ward and find a computer.

## Why Flutter?

Staff normally work close to the actual assets.

A mobile phone allows them to:

- Scan an asset
- Find an asset
- Report a problem
- Take a photo
- Submit the problem immediately

## Why No React?

Staff do not need complex management screens.

Their work is mainly simple field activities using assets around them.

## What Staff Cannot Do

Staff have the most limited management permissions.

They cannot:

- Manage users
- Configure departments
- Configure asset categories or types
- Approve transfers
- Approve disposals
- Manage verification campaigns
- Manage organisation settings
- Perform Auditor responsibilities

Staff mainly need to **find assets and report problems**.

## Typical Day

**Find problem → Open Flutter → Scan asset → Take photo → Describe problem → Submit maintenance request**

### Simple Idea

> **Staff = Find assets and report problems → Flutter**

---

---

## 3. Responsibility Boundaries

This table shows where each role's responsibility stops.

| RoleMain ResponsibilityShould Not Normally Do |                                    |                                                               |
| --------------------------------------------- | ---------------------------------- | ------------------------------------------------------------- |
| Administrator                                 | Manage the system                  | Routine field scanning and verification                       |
| Auditor                                       | Review and audit                   | Perform Inventory Officer's field verification                |
| Inventory Officer                             | Manage and physically check assets | Administrator-only configuration and independent audit review |
| Staff                                         | Find assets and report problems    | Administration, approvals and auditing                        |

These boundaries help keep responsibilities clear and reduce unnecessary access.

---

---

## 4. Common Scenarios by Sri Lankan Organisation

The following scenarios show how the same CoreGrid role model can be applied in different public-sector environments. These are illustrative examples; they do not mean that the named organisations currently use CoreGrid.

## Ministry of Education

## Example Environment

CoreGrid could be used to manage assets across:

- Government schools
- Education offices
- Computer laboratories
- Science laboratories
- School libraries
- Administrative offices

Example assets:

- Desktop computers
- Laptops
- Smart boards
- Projectors
- Printers
- Laboratory equipment
- Desks and chairs
- Sports equipment
- Network equipment

### Administrator

#### Scenario 1 — Create School Departments

An Administrator creates locations and departments such as:

- Administration Office
- ICT Laboratory
- Science Laboratory
- Library
- Staff Room

**Platform:** React

#### Scenario 2 — Configure a New Asset Type

A school receives new smart boards.

The Administrator creates:

> Asset Type: Smart Board

and defines information such as:

- Manufacturer
- Model
- Serial number
- Warranty period
- Installation date

**Platform:** React

#### Scenario 3 — Create Users

A new ICT staff member joins the school.

The Administrator creates the user account and assigns the correct CoreGrid role.

**Platform:** React

#### Scenario 4 — Approve Asset Disposal

An old computer has reached the end of its useful life and has been approved for disposal through the required process.

The Administrator reviews the information and approves the disposal.

**Platform:** React

---

### Auditor

#### Scenario 1 — Computer Laboratory Audit

The Auditor creates a verification campaign:

> School ICT Laboratory — Computer Verification 2026

The Inventory Officer physically checks the computers.

The Auditor later reviews the results.

**Platform:** React

#### Scenario 2 — Investigate Missing Equipment

The verification results show:

> Projector — Missing

The Auditor checks:

- Asset history
- Previous location
- Verification result
- Audit log
- Related discrepancy

**Platform:** React

#### Scenario 3 — Review Location Mismatch

The system says:

> Laptop Location: ICT Laboratory

But the physical verification says:

> Laptop Location: Administration Office

The Auditor reviews the location discrepancy.

**Platform:** React

#### Scenario 4 — Generate Audit Report

After completing the verification campaign, the Auditor prepares the final report for management.

**Platform:** React

---

### Inventory Officer

#### Scenario 1 — Register New Computers

The school receives 30 new computers.

The Inventory Officer uses React to register them.

**Platform:** React

#### Scenario 2 — Scan Computers

The Inventory Officer walks through the ICT laboratory and scans each computer's QR code.

**Platform:** Flutter

#### Scenario 3 — Verify Equipment

The Inventory Officer checks:

- Correct location
- Current condition
- Asset status
- Physical presence

**Platform:** Flutter

#### Scenario 4 — Record Maintenance

A projector needs a new lamp.

The Inventory Officer creates and manages the maintenance record.

**Platform:** React

#### Scenario 5 — Receive Transferred Equipment

A laptop is transferred from another department or location.

The Inventory Officer physically receives and confirms the asset.

**Platform:** Flutter

---

### Staff

#### Scenario 1 — Report Broken Projector

A teacher finds that a classroom projector is not working.

They scan the asset and submit a fault report.

**Platform:** Flutter

#### Scenario 2 — Report Computer Problem

A staff member finds that a laboratory computer will not start.

They:

1. Scan the QR code.
2. Open the asset.
3. Describe the problem.
4. Take a photo if required.
5. Submit the report.

**Platform:** Flutter

#### Scenario 3 — Find Asset Information

A staff member needs basic information about a printer.

They scan its QR code and view the permitted asset information.

**Platform:** Flutter

---

## Ministry of Health

## Example Environment

CoreGrid could be used in:

- District General Hospitals
- Teaching Hospitals
- Base Hospitals
- Medical units
- Hospital wards
- Biomedical Engineering units

Example assets:

- Ventilators
- Infusion pumps
- Patient monitors
- MRI machines
- Ultrasound scanners
- X-ray equipment
- Hospital beds
- Computers
- Ambulances

---

### Administrator

#### Scenario 1 — Configure Hospital Departments

The Administrator creates:

- ICU
- Cardiology
- Radiology
- Emergency Treatment Unit
- Operating Theatre
- Ward 4
- Biomedical Engineering Unit

**Platform:** React

#### Scenario 2 — Configure Medical Equipment

The hospital receives a new type of patient monitor.

The Administrator creates the asset type and its required attributes.

**Platform:** React

#### Scenario 3 — Manage Hospital Users

A new Biomedical Engineering technician joins the hospital.

The Administrator creates the account and assigns the correct role.

**Platform:** React

#### Scenario 4 — Approve Disposal

An old medical device has been condemned and completed the required disposal process.

The Administrator reviews the information and approves the disposal.

**Platform:** React

---

### Auditor

#### Scenario 1 — ICU Equipment Verification

The Auditor creates:

> ICU Medical Equipment Verification — 2026

The Inventory Officer physically verifies the equipment.

**Platform:** React

#### Scenario 2 — Missing Medical Equipment

The verification campaign reports:

> Infusion Pump — Missing

The Auditor checks the asset history and discrepancy.

**Platform:** React

#### Scenario 3 — Condition Mismatch

CoreGrid says:

> Patient Monitor — GOOD

Physical verification says:

> Patient Monitor — DAMAGED

The Auditor investigates the mismatch.

**Platform:** React

#### Scenario 4 — Review Asset History

The Auditor checks:

- Who changed the condition
- When it was changed
- Previous location
- Maintenance history
- Verification history

**Platform:** React

---

### Inventory Officer

#### Scenario 1 — Register Medical Equipment

The hospital receives new infusion pumps.

The Inventory Officer registers them.

**Platform:** React

#### Scenario 2 — Scan Equipment in a Ward

The Inventory Officer visits Ward 4 and scans equipment QR codes.

**Platform:** Flutter

#### Scenario 3 — Verify Equipment Condition

The Inventory Officer checks whether a patient monitor is:

- Present
- In the correct ward
- In the correct condition

**Platform:** Flutter

#### Scenario 4 — Create Maintenance Record

A ventilator requires servicing.

The Inventory Officer creates the maintenance record and records the estimated cost.

**Platform:** React

#### Scenario 5 — Complete Maintenance

After the repair, the Inventory Officer records:

- Actual cost
- Completion information
- New condition

**Platform:** React

---

### Staff

#### Scenario 1 — Report Infusion Pump Fault

A ward attendant notices an unusual sound from an infusion pump.

They scan it and report the problem.

**Platform:** Flutter

#### Scenario 2 — Report Damaged Hospital Bed

A hospital bed has a damaged wheel.

Staff:

1. Scan the bed.
2. Take a photo.
3. Describe the problem.
4. Submit the fault.

**Platform:** Flutter

#### Scenario 3 — Identify Equipment

Staff find medical equipment and need to confirm its asset information.

They scan its QR code.

**Platform:** Flutter

---

## Ministry of Transport and Highways

## Example Environment

CoreGrid could support asset management environments such as:

- SLTB depots
- Sri Lanka Railways facilities
- Transport offices
- Workshops
- Road-related operational facilities

Example assets:

- Buses
- Workshop equipment
- Computers
- Generators
- Tools
- Railway equipment
- Service vehicles
- Road maintenance equipment

---

### Administrator

#### Scenario 1 — Configure Depot Locations

The Administrator creates:

- Main Office
- Bus Yard
- Mechanical Workshop
- Electrical Workshop
- Stores
- Fuel Area

**Platform:** React

#### Scenario 2 — Configure Bus Asset Type

The Administrator creates:

> Asset Type: Bus

Possible attributes include:

- Registration number
- Chassis number
- Engine number
- Manufacturer
- Model
- Year

**Platform:** React

#### Scenario 3 — Manage Users

A new Inventory Officer joins the depot.

The Administrator creates the account and assigns the Inventory Officer role.

**Platform:** React

#### Scenario 4 — Review Disposal

An old bus has been condemned and completed the required disposal process.

The Administrator reviews the information before approval.

**Platform:** React

---

### Auditor

#### Scenario 1 — Bus Fleet Verification

The Auditor creates:

> Kottawa Depot — Bus Fleet Verification — Q3 2026

**Platform:** React

#### Scenario 2 — Missing Bus Investigation

CoreGrid shows a bus in the register, but the verification result says:

> Missing

The Auditor reviews the discrepancy.

**Platform:** React

#### Scenario 3 — Location Mismatch

CoreGrid says:

> Bus Location: Kottawa Depot

Physical verification says:

> Bus Location: Maharagama Depot

The Auditor investigates the location mismatch.

**Platform:** React

#### Scenario 4 — Condition Mismatch

CoreGrid says:

> Bus Condition: GOOD

Physical verification says:

> Bus Condition: NEEDS REPAIR

The Auditor reviews the discrepancy.

**Platform:** React

#### Scenario 5 — Audit Report

After the campaign is completed, the Auditor reviews the final results and prepares the required report.

**Platform:** React

---

### Inventory Officer

#### Scenario 1 — Register New Bus

A new bus arrives at the depot.

The Inventory Officer registers it in CoreGrid.

**Platform:** React

#### Scenario 2 — Scan Bus in the Yard

The Inventory Officer walks to the bus and scans its QR code.

**Platform:** Flutter

#### Scenario 3 — Verify Bus Condition

The Inventory Officer checks:

- Physical presence
- Location
- Condition
- Asset information

**Platform:** Flutter

#### Scenario 4 — Create Maintenance Record

Bus **NB-4471** needs new brake pads.

The Inventory Officer creates a maintenance record.

**Platform:** React

#### Scenario 5 — Record Maintenance Cost

The Inventory Officer enters:

- Estimated cost
- Assigned person
- Maintenance information

**Platform:** React

#### Scenario 6 — Confirm Transfer

A bus arrives after being transferred from another depot.

The Inventory Officer physically checks it and confirms receipt.

**Platform:** Flutter

#### Scenario 7 — Complete Verification Task

An Auditor-created campaign assigns 40 buses to the Inventory Officer.

The Officer walks through the yard and verifies each assigned bus.

**Platform:** Flutter

---

### Staff

#### Scenario 1 — Report Bus Problem

A depot worker notices damage to a bus.

They scan the bus and submit a fault report.

**Platform:** Flutter

#### Scenario 2 — Report Workshop Equipment Problem

A workshop worker finds that a machine is not working correctly.

They:

1. Scan the equipment.
2. Take a photo.
3. Describe the problem.
4. Submit the report.

**Platform:** Flutter

#### Scenario 3 — Identify an Asset

A worker needs to check the basic details of a piece of workshop equipment.

They scan its QR code.

**Platform:** Flutter

---

---

## 5. Cross-Organisation Comparison

| Role | Ministry of Education | Ministry of Health | Ministry of Transport and Highways | Platform |
|---|---|---|---|---|
| Administrator | Configure schools, locations, users, and equipment types | Configure hospital departments, users, and medical equipment types | Configure depots, users, and transport asset types | React |
| Auditor | Review school equipment verification and discrepancies | Review medical equipment verification and discrepancies | Review fleet/equipment verification and discrepancies | React |
| Inventory Officer | Register, maintain, scan, and verify school assets | Register, maintain, scan, and verify medical equipment | Register, maintain, scan, and verify buses/equipment | React + Flutter |
| Staff | Identify assets and report classroom/equipment faults | Identify equipment and report ward/equipment faults | Identify assets and report bus/workshop faults | Flutter |

---

## 6. Final Platform Rule

| Role | Platform | Reason |
|---|---|---|
| Administrator | React | Performs system configuration, user management, and approval work |
| Auditor | React | Reviews campaigns, discrepancies, audit history, and reports |
| Inventory Officer | React + Flutter | Performs both office asset management and physical field work |
| Staff | Flutter | Performs simple asset lookup and fault reporting close to the asset |

The organisation and asset types can change, but the platform rule remains based on the **responsibility and working environment of the role**.

> **Implementation note:** Current implementation status should remain in `doc/PROGRESS.md` rather than being repeated in this guide. This avoids duplicated information becoming outdated.
